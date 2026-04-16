using Microsoft.Extensions.DependencyInjection;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Realtime;
using TeacherAppointment.Infrastructure.Integrations.Email;
using TeacherAppointment.Infrastructure.Integrations.QrCodes;
using TeacherAppointment.Infrastructure.Persistence;
using TeacherAppointment.Infrastructure.Realtime;

namespace TeacherAppointment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.AddSingleton<IQrCodeGenerator, PlaceholderQrCodeGenerator>();
        services.AddSingleton<IAuthChallengeNotifier, SignalRAuthChallengeNotifier>();
        services.AddSingleton<ITeacherRepository, InMemoryTeacherRepository>();
        services.AddSingleton<IAuthChallengeRepository, InMemoryAuthChallengeRepository>();
        services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();

        return services;
    }
}
