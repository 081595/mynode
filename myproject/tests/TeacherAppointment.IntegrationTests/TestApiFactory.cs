using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace TeacherAppointment.IntegrationTests;

public sealed class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath;

    public TestApiFactory()
    {
        var dbDir = Path.Combine(Path.GetTempPath(), "teacher-appointment-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dbDir);
        _dbPath = Path.Combine(dbDir, "teacher-appointment.integration.db");
    }

    public string DbPath => _dbPath;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sqlite:ConnectionString"] = $"Data Source={_dbPath}",
                ["AuthCookies:AccessTokenName"] = "ta_access_token_it",
                ["AuthCookies:RefreshTokenName"] = "ta_refresh_token_it"
            });
        });
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task InitializeAsync()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();

        var dbDir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrWhiteSpace(dbDir) && Directory.Exists(dbDir))
        {
            Directory.Delete(dbDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    public async Task<string> GetVerificationCodeAsync(string challengeId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT verification_code FROM auth_challenges WHERE challenge_id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", challengeId);

        var value = await command.ExecuteScalarAsync();
        return Convert.ToString(value) ?? throw new InvalidOperationException("verification_code not found");
    }

    public async Task ExpireChallengeAsync(string challengeId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE auth_challenges SET expires_at_utc = $expiresAt WHERE challenge_id = $id;";
        command.Parameters.AddWithValue("$expiresAt", DateTime.UtcNow.AddMinutes(-10).ToString("O"));
        command.Parameters.AddWithValue("$id", challengeId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> GetDownloadCountAsync(int year, string employeeNo, int docYear, string docType, string docSeq)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT download_count
FROM teach_appo_resp
WHERE yr = $yr AND empl_no = $emplNo AND appo_doc_yy = $docYear AND appo_doc_ch = $docType AND appo_doc_seq = $docSeq
LIMIT 1;
""";
        command.Parameters.AddWithValue("$yr", year);
        command.Parameters.AddWithValue("$emplNo", employeeNo);
        command.Parameters.AddWithValue("$docYear", docYear);
        command.Parameters.AddWithValue("$docType", docType);
        command.Parameters.AddWithValue("$docSeq", docSeq);

        var scalar = await command.ExecuteScalarAsync();
        return Convert.ToInt32(scalar);
    }

    public static async Task<string> ExtractChallengeIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("challengeId", out var challengeIdElement))
        {
            throw new InvalidOperationException("challengeId not found in response");
        }

        return challengeIdElement.GetString() ?? throw new InvalidOperationException("challengeId is null");
    }

    public static StringContent JsonBody(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    public static async Task AssertStatusWithBodyAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new Xunit.Sdk.XunitException($"Expected {(int)expected}, got {(int)response.StatusCode}: {body}");
    }

    public static (string? AccessToken, string? RefreshToken) ExtractTokensFromSetCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            return (null, null);
        }

        string? access = null;
        string? refresh = null;

        foreach (var cookie in cookieHeaders)
        {
            if (cookie.StartsWith("ta_access_token_it=", StringComparison.Ordinal))
            {
                access = cookie.Split(';', 2)[0].Split('=', 2)[1];
            }

            if (cookie.StartsWith("ta_refresh_token_it=", StringComparison.Ordinal))
            {
                refresh = cookie.Split(';', 2)[0].Split('=', 2)[1];
            }
        }

        return (access, refresh);
    }

    public static void AttachSession(HttpClient client, string? accessToken, string? refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", $"ta_refresh_token_it={refreshToken}");
        }
    }
}
