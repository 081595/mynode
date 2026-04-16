using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Infrastructure.Security;

public sealed class InMemoryRateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, CounterWindow> _windows = new(StringComparer.Ordinal);
    private readonly RateLimitOptions _options;

    public InMemoryRateLimitService(IOptions<RateLimitOptions> options)
    {
        _options = options.Value;
    }

    public RateLimitDecision Check(RateLimitRequest request)
    {
        var now = request.TimestampUtc;
        var configured = GetConfiguredLimit(request.Action);
        if (configured is null)
        {
            return new RateLimitDecision(true, null, null, null);
        }

        var identityKey = $"id:{request.Action}:{Normalize(request.IdentityKey)}";
        var clientKey = $"ip:{request.Action}:{Normalize(request.ClientIp)}";

        var identityDecision = CheckKey(identityKey, configured, now, "identity", request.Action);
        if (!identityDecision.Allowed)
        {
            return identityDecision;
        }

        return CheckKey(clientKey, configured, now, "client", request.Action);
    }

    private RateLimitDecision CheckKey(string key, ActionLimit limit, DateTime now, string scope, string action)
    {
        var window = _windows.GetOrAdd(key, _ => new CounterWindow(now, 0));

        lock (window.SyncRoot)
        {
            var elapsed = now - window.WindowStartUtc;
            if (elapsed >= limit.Window)
            {
                window.WindowStartUtc = now;
                window.Count = 0;
            }

            if (window.Count >= limit.MaxAttempts)
            {
                var retryAfter = limit.Window - (now - window.WindowStartUtc);
                if (retryAfter < TimeSpan.Zero)
                {
                    retryAfter = TimeSpan.Zero;
                }

                return new RateLimitDecision(
                    false,
                    retryAfter,
                    scope,
                    $"rate_limited_{action}_{scope}");
            }

            window.Count++;
            return new RateLimitDecision(true, null, null, null);
        }
    }

    private ActionLimit? GetConfiguredLimit(string action)
    {
        return action switch
        {
            "identify" => _options.Identify,
            "challenge_resend" => _options.ChallengeResend,
            _ => null
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant();
    }

    private sealed class CounterWindow
    {
        public CounterWindow(DateTime windowStartUtc, int count)
        {
            WindowStartUtc = windowStartUtc;
            Count = count;
        }

        public object SyncRoot { get; } = new();

        public DateTime WindowStartUtc { get; set; }

        public int Count { get; set; }
    }
}
