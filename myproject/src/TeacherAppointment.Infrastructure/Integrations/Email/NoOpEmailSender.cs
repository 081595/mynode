using TeacherAppointment.Application.Abstractions.Infrastructure;

namespace TeacherAppointment.Infrastructure.Integrations.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendVerificationCodeAsync(string targetEmail, string code, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
