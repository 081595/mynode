using Microsoft.Extensions.DependencyInjection;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Abstractions.Realtime;
using TeacherAppointment.Infrastructure.Integrations.Email;
using TeacherAppointment.Infrastructure.Integrations.QrCodes;
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

        return services;
    }
}
