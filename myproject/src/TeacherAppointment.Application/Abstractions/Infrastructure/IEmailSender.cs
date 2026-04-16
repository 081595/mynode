namespace TeacherAppointment.Application.Abstractions.Infrastructure;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string targetEmail, string code, CancellationToken cancellationToken = default);
}
