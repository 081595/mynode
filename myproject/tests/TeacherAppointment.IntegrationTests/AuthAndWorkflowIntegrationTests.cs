using System.Net;

namespace TeacherAppointment.IntegrationTests;

public sealed class AuthAndWorkflowIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthAndWorkflowIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EmailAuthFlow_WithTokenLifecycle_AndWorkflowAndAdminRules_Works()
    {
        using var teacherClient = _factory.CreateApiClient();

        var identifyResponse = await teacherClient.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"A123456789\"," +
                                    "\"birthday\":\"1985-03-17\"" +
                                    "}"));

        var challengeId = await TestApiFactory.ExtractChallengeIdAsync(identifyResponse);
        var verificationCode = await _factory.GetVerificationCodeAsync(challengeId);

        var verifyResponse = await teacherClient.PostAsync(
            "/api/auth/verify-email",
            TestApiFactory.JsonBody("{" +
                                    $"\"challengeId\":\"{challengeId}\"," +
                                    $"\"code\":\"{verificationCode}\"" +
                                    "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(verifyResponse, HttpStatusCode.OK);

        var exchangeResponse = await teacherClient.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(exchangeResponse, HttpStatusCode.OK);
        var (teacherAccessToken, teacherRefreshToken) = TestApiFactory.ExtractTokensFromSetCookie(exchangeResponse);
        TestApiFactory.AttachSession(teacherClient, teacherAccessToken, teacherRefreshToken);

        var listResponse = await teacherClient.GetAsync("/api/appointments?year=115");
        await TestApiFactory.AssertStatusWithBodyAsync(listResponse, HttpStatusCode.OK);

        var beforeDownloadCount = await _factory.GetDownloadCountAsync(115, "E12345", 115, "教字", "0001");
        var teacherPdfResponse = await teacherClient.GetAsync("/api/appointments/115/E12345/115/教字/0001/pdf");
        await TestApiFactory.AssertStatusWithBodyAsync(teacherPdfResponse, HttpStatusCode.OK);
        var afterTeacherDownloadCount = await _factory.GetDownloadCountAsync(115, "E12345", 115, "教字", "0001");
        Assert.Equal(beforeDownloadCount + 1, afterTeacherDownloadCount);

        var completeResponse = await teacherClient.PostAsync("/api/appointments/115/E12345/115/教字/0001/complete", null);
        await TestApiFactory.AssertStatusWithBodyAsync(completeResponse, HttpStatusCode.OK);

        using var adminClient = _factory.CreateApiClient();
        var adminIdentifyResponse = await adminClient.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"B223456789\"," +
                                    "\"birthday\":\"1978-10-04\"" +
                                    "}"));
        var adminChallengeId = await TestApiFactory.ExtractChallengeIdAsync(adminIdentifyResponse);
        var adminCode = await _factory.GetVerificationCodeAsync(adminChallengeId);

        await TestApiFactory.AssertStatusWithBodyAsync(
            await adminClient.PostAsync(
                "/api/auth/verify-email",
                TestApiFactory.JsonBody("{" +
                                        $"\"challengeId\":\"{adminChallengeId}\"," +
                                        $"\"code\":\"{adminCode}\"" +
                                        "}")),
            HttpStatusCode.OK);

        var adminExchangeResponse = await adminClient.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{adminChallengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(adminExchangeResponse, HttpStatusCode.OK);
        var (adminAccessToken, adminRefreshToken) = TestApiFactory.ExtractTokensFromSetCookie(adminExchangeResponse);
        TestApiFactory.AttachSession(adminClient, adminAccessToken, adminRefreshToken);

        var beforeAdminPreviewCount = await _factory.GetDownloadCountAsync(115, "E12345", 115, "教字", "0001");
        var adminPreview = await adminClient.GetAsync("/api/appointments/115/E12345/115/教字/0001/pdf");
        await TestApiFactory.AssertStatusWithBodyAsync(adminPreview, HttpStatusCode.OK);
        var afterAdminPreviewCount = await _factory.GetDownloadCountAsync(115, "E12345", 115, "教字", "0001");
        Assert.Equal(beforeAdminPreviewCount, afterAdminPreviewCount);

        var teacherDeniedAdminCall = await teacherClient.GetAsync("/api/admin/teachers?year=115");
        Assert.Equal(HttpStatusCode.Forbidden, teacherDeniedAdminCall.StatusCode);

        var refreshResponse = await teacherClient.PostAsync("/api/auth/sessions/refresh", null);
        await TestApiFactory.AssertStatusWithBodyAsync(refreshResponse, HttpStatusCode.OK);

        var logoutResponse = await teacherClient.PostAsync("/api/auth/logout", null);
        await TestApiFactory.AssertStatusWithBodyAsync(logoutResponse, HttpStatusCode.OK);
    }

    [Fact]
    public async Task NegativeAndSecurityScenarios_AreRejectedAsExpected()
    {
        using var client = _factory.CreateApiClient();

        var invalidIdentity = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"Z999999999\"," +
                                    "\"birthday\":\"1990-01-01\"" +
                                    "}"));
        Assert.Equal(HttpStatusCode.Unauthorized, invalidIdentity.StatusCode);

        var validIdentify = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"A123456789\"," +
                                    "\"birthday\":\"1985-03-17\"" +
                                    "}"));
        var challengeId = await TestApiFactory.ExtractChallengeIdAsync(validIdentify);

        var abuseResend = await client.PostAsync($"/api/auth/challenges/{challengeId}/resend", null);
        Assert.Equal((HttpStatusCode)429, abuseResend.StatusCode);

        await _factory.ExpireChallengeAsync(challengeId);
        var code = await _factory.GetVerificationCodeAsync(challengeId);

        var expiredVerify = await client.PostAsync(
            "/api/auth/verify-email",
            TestApiFactory.JsonBody("{" +
                                    $"\"challengeId\":\"{challengeId}\"," +
                                    $"\"code\":\"{code}\"" +
                                    "}"));

        Assert.Equal(HttpStatusCode.BadRequest, expiredVerify.StatusCode);
    }
}
