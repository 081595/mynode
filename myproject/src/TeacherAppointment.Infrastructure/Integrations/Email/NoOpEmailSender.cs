using TeacherAppointment.Application.Abstractions.Infrastructure;
using Microsoft.Extensions.Logging;

namespace TeacherAppointment.Infrastructure.Integrations.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string targetEmail, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NoOp email sender issued verification code {Code} for {TargetEmail}", code, targetEmail);
        return Task.CompletedTask;
    }
}
