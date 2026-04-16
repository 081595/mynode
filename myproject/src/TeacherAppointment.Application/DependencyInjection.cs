using Microsoft.Extensions.DependencyInjection;
using TeacherAppointment.Application.Features.Auth;
using TeacherAppointment.Application.Features.AdminMaintenance;

namespace TeacherAppointment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IIdentityChallengeService, IdentityChallengeService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IAdminMaintenanceService, AdminMaintenanceService>();

        return services;
    }
}
