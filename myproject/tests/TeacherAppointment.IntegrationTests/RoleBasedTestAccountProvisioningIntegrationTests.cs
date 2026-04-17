using System.Net;

namespace TeacherAppointment.IntegrationTests;

public sealed class RoleBasedTestAccountProvisioningIntegrationTests
{
    [Fact]
    public async Task SeededUserAndAdminAccounts_CanAuthenticateWithExistingFlow()
    {
        using var factory = TestApiFactory.Create(enableRoleBasedProvisioning: true, environmentName: "Development");
        using var client = factory.CreateApiClient();

        await EnsureHealthyAsync(client);

        var userIdentify = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"A123456789\"," +
                                    "\"birthday\":\"1985-03-17\"" +
                                    "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(userIdentify, HttpStatusCode.OK);

        var adminIdentify = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"B223456789\"," +
                                    "\"birthday\":\"1978-10-04\"" +
                                    "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(adminIdentify, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Guardrails_DisabledFlagOrIneligibleEnvironment_SkipProvisioning()
    {
        using (var disabledFactory = TestApiFactory.Create(enableRoleBasedProvisioning: false, environmentName: "Development"))
        {
            using var disabledClient = disabledFactory.CreateApiClient();
            await EnsureHealthyAsync(disabledClient);

            Assert.Equal(0, await disabledFactory.CountTeachersByEmployeeNoAsync("TST-U-0001"));
            Assert.Equal(0, await disabledFactory.CountTeachersByEmployeeNoAsync("TST-A-0001"));

            var identify = await disabledClient.PostAsync(
                "/api/auth/identify",
                TestApiFactory.JsonBody("{" +
                                        "\"idNo\":\"A123456789\"," +
                                        "\"birthday\":\"1985-03-17\"" +
                                        "}"));
            Assert.Equal(HttpStatusCode.Unauthorized, identify.StatusCode);
        }

        using (var ineligibleFactory = TestApiFactory.Create(enableRoleBasedProvisioning: true, environmentName: "Production"))
        {
            using var ineligibleClient = ineligibleFactory.CreateApiClient();
            await EnsureHealthyAsync(ineligibleClient);

            Assert.Equal(0, await ineligibleFactory.CountTeachersByEmployeeNoAsync("TST-U-0001"));
            Assert.Equal(0, await ineligibleFactory.CountTeachersByEmployeeNoAsync("TST-A-0001"));
        }
    }

    [Fact]
    public async Task Provisioning_Rerun_DoesNotCreateDuplicateRoleAccounts()
    {
        var dbDir = Path.Combine(Path.GetTempPath(), "teacher-appointment-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "teacher-appointment.integration.db");

        using (var firstFactory = TestApiFactory.Create(
                   dbPath: dbPath,
                   cleanupOnDispose: false,
                   environmentName: "Development",
                   enableRoleBasedProvisioning: true))
        {
            using var firstClient = firstFactory.CreateApiClient();
            await EnsureHealthyAsync(firstClient);

            Assert.Equal(1, await firstFactory.CountTeachersByEmployeeNoAsync("TST-U-0001"));
            Assert.Equal(1, await firstFactory.CountTeachersByEmployeeNoAsync("TST-A-0001"));
        }

        using (var secondFactory = TestApiFactory.Create(
                   dbPath: dbPath,
                   cleanupOnDispose: true,
                   environmentName: "Development",
                   enableRoleBasedProvisioning: true))
        {
            using var secondClient = secondFactory.CreateApiClient();
            await EnsureHealthyAsync(secondClient);

            Assert.Equal(1, await secondFactory.CountTeachersByEmployeeNoAsync("TST-U-0001"));
            Assert.Equal(1, await secondFactory.CountTeachersByEmployeeNoAsync("TST-A-0001"));
        }
    }

    private static async Task EnsureHealthyAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }
}
