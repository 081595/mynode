using Microsoft.Extensions.DependencyInjection;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IIdentityChallengeService, IdentityChallengeService>();

        return services;
    }
}
