using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace TeacherAppointment.IntegrationTests;

public sealed class QrSignalRIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public QrSignalRIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QrFlow_EmitsDesktopAndMobileSignalREvents_AndAllowsSessionExchange()
    {
        using var client = _factory.CreateApiClient();

        var identifyResponse = await client.PostAsync(
            "/api/auth/identify",
            TestApiFactory.JsonBody("{" +
                                    "\"idNo\":\"A123456789\"," +
                                    "\"birthday\":\"1985-03-17\"" +
                                    "}"));
        var challengeId = await TestApiFactory.ExtractChallengeIdAsync(identifyResponse);

        var qrCreateResponse = await client.PostAsync(
            "/api/auth/qr-sessions",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(qrCreateResponse, HttpStatusCode.OK);

        var payload = await qrCreateResponse.Content.ReadAsStringAsync();
        var sessionId = System.Text.Json.JsonDocument.Parse(payload).RootElement.GetProperty("sessionId").GetString()
            ?? throw new InvalidOperationException("Missing sessionId");

        var desktopEvent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mobileEvent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var desktopHub = BuildHubConnection(client, "desktop-client");
        var mobileHub = BuildHubConnection(client, "mobile-client");

        desktopHub.On<object>("desktop.redirect", _ => desktopEvent.TrySetResult("ok"));
        mobileHub.On<object>("mobile.close", _ => mobileEvent.TrySetResult("ok"));

        await desktopHub.StartAsync();
        await mobileHub.StartAsync();

        await desktopHub.InvokeAsync("JoinDesktopSessionAsync", sessionId);
        await mobileHub.InvokeAsync("JoinMobileSessionAsync", sessionId);

        var confirmResponse = await client.PostAsync($"/api/auth/qr-sessions/{sessionId}/confirm", null);
        await TestApiFactory.AssertStatusWithBodyAsync(confirmResponse, HttpStatusCode.OK);

        var desktopReceived = await WaitForSignalAsync(desktopEvent.Task, TimeSpan.FromSeconds(5));
        var mobileReceived = await WaitForSignalAsync(mobileEvent.Task, TimeSpan.FromSeconds(5));
        Assert.True(desktopReceived);
        Assert.True(mobileReceived);

        await desktopHub.DisposeAsync();
        await mobileHub.DisposeAsync();

        var exchangeResponse = await client.PostAsync(
            "/api/auth/sessions/exchange",
            TestApiFactory.JsonBody("{" + $"\"challengeId\":\"{challengeId}\"" + "}"));
        await TestApiFactory.AssertStatusWithBodyAsync(exchangeResponse, HttpStatusCode.OK);
    }

    private HubConnection BuildHubConnection(HttpClient apiClient, string userAgent)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(apiClient.BaseAddress!, "/hubs/auth-challenge"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("User-Agent", userAgent);
            })
            .WithAutomaticReconnect()
            .Build();
    }

    private static async Task<bool> WaitForSignalAsync(Task<string> task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        return completedTask == task;
    }
}
