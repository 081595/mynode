using System.Net;

namespace TeacherAppointment.IntegrationTests;

public sealed class RazorPagesIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public RazorPagesIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicPages_AreAvailable_AndProtectedPageRedirectsWhenAnonymous()
    {
        using var client = _factory.CreateApiClient();

        var index = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);

        var login = await client.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var teacher = await client.GetAsync("/Teacher/Index");
        Assert.Equal(HttpStatusCode.Redirect, teacher.StatusCode);
        Assert.Contains("/Auth/Login", teacher.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task TeacherSession_CanAccessTeacherWorkspace_ButCannotOpenAdminWorkspace()
    {
        using var teacherClient = _factory.CreateApiClient();

        var challengeId = await StartAndVerifyByEmailAsync(teacherClient, "A123456789", "1985-03-17");
        var exchangeResponse = await teacherClient.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(exchangeResponse, HttpStatusCode.OK);

        var (access, refresh) = TestApiFactory.ExtractTokensFromSetCookie(exchangeResponse);
        TestApiFactory.AttachSession(teacherClient, access, refresh);

        var teacherPage = await teacherClient.GetAsync("/Teacher/Index?year=115");
        Assert.Equal(HttpStatusCode.OK, teacherPage.StatusCode);

        var adminPage = await teacherClient.GetAsync("/Admin/Teachers?year=115");
        Assert.Equal(HttpStatusCode.Redirect, adminPage.StatusCode);
        Assert.Contains("/", adminPage.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminSession_CanAccessAdminWorkspaces_AndVerifyPageContainsRealtimeFallbackMessage()
    {
        using var adminClient = _factory.CreateApiClient();

        var challengeId = await StartAndVerifyByEmailAsync(adminClient, "B223456789", "1978-10-04");
        var exchangeResponse = await adminClient.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(exchangeResponse, HttpStatusCode.OK);

        var (access, refresh) = TestApiFactory.ExtractTokensFromSetCookie(exchangeResponse);
        TestApiFactory.AttachSession(adminClient, access, refresh);

        var teachersPage = await adminClient.GetAsync("/Admin/Teachers?year=115");
        Assert.Equal(HttpStatusCode.OK, teachersPage.StatusCode);

        var appointmentsPage = await adminClient.GetAsync("/Admin/Appointments?year=115");
        Assert.Equal(HttpStatusCode.OK, appointmentsPage.StatusCode);

        var verifyPage = await adminClient.GetAsync($"/Auth/Verify?challengeId={challengeId}");
        Assert.Equal(HttpStatusCode.OK, verifyPage.StatusCode);
        var html = await verifyPage.Content.ReadAsStringAsync();
        Assert.Contains("即時通道不可用", html);
    }

    private async Task<string> StartAndVerifyByEmailAsync(HttpClient client, string idNo, string birthday)
    {
        var identifyResponse = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    $"\"idNo\":\"{idNo}\"," +
                                    $"\"birthday\":\"{birthday}\"" +
                                    "}"));
        var challengeId = await TestApiFactory.ExtractChallengeIdAsync(identifyResponse);
        var code = await _factory.GetVerificationCodeAsync(challengeId);

        var verifyResponse = await client.PostAsync(
            "/api/auth/verify-email",
            TestApiFactory.JsonBody("{" +
                                    $"\"challengeId\":\"{challengeId}\"," +
                                    $"\"code\":\"{code}\"" +
                                    "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(verifyResponse, HttpStatusCode.OK);
        return challengeId;
    }
}
