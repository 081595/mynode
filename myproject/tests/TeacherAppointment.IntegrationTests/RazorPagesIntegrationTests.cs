using System.Net;
using System.Text.RegularExpressions;

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

    [Fact]
    public async Task Logout_ThenHome_DoesNotShowAuthenticatedBadge()
    {
        using var adminClient = _factory.CreateApiClient();

        var challengeId = await StartAndVerifyByEmailAsync(adminClient, "B223456789", "1978-10-04");
        var exchangeResponse = await adminClient.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(exchangeResponse, HttpStatusCode.OK);

        var (accessToken, refreshToken) = TestApiFactory.ExtractTokensFromSetCookie(exchangeResponse);
        TestApiFactory.AttachSession(adminClient, accessToken, refreshToken);

        var beforeLogoutHome = await adminClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, beforeLogoutHome.StatusCode);
        var beforeLogoutHtml = await beforeLogoutHome.Content.ReadAsStringAsync();
        Assert.Contains("TST-A-0001", beforeLogoutHtml);
        Assert.Contains("Admin User", beforeLogoutHtml);

        var token = ExtractRequestVerificationToken(beforeLogoutHtml);
        var logoutResponse = await adminClient.PostAsync(
            "/Auth/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        var location = logoutResponse.Headers.Location?.OriginalString;
        Assert.True(
            string.Equals(location, "/", StringComparison.Ordinal) ||
            string.Equals(location, "/Index", StringComparison.OrdinalIgnoreCase),
            $"Unexpected logout redirect location: {location}");

        if (logoutResponse.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            Assert.Contains(cookieHeaders, value => value.StartsWith("ta_access_token_it=;", StringComparison.Ordinal));
            Assert.Contains(cookieHeaders, value => value.StartsWith("ta_refresh_token_it=;", StringComparison.Ordinal));
        }
        else
        {
            throw new Xunit.Sdk.XunitException("Logout response did not include Set-Cookie headers.");
        }

        adminClient.DefaultRequestHeaders.Authorization = null;
        adminClient.DefaultRequestHeaders.Remove("Cookie");

        var afterLogoutHome = await adminClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, afterLogoutHome.StatusCode);
        var afterLogoutHtml = await afterLogoutHome.Content.ReadAsStringAsync();

        Assert.DoesNotContain("TST-A-0001", afterLogoutHtml);
        Assert.DoesNotContain("Admin User", afterLogoutHtml);
        Assert.DoesNotContain("B2*****89", afterLogoutHtml);
        Assert.Contains("開始登入", afterLogoutHtml);
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

    private static string ExtractRequestVerificationToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            throw new Xunit.Sdk.XunitException("Unable to find __RequestVerificationToken in page HTML.");
        }

        return match.Groups["token"].Value;
    }
}
