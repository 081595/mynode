using Microsoft.Extensions.DependencyInjection;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Realtime;
using Microsoft.Extensions.Configuration;
using TeacherAppointment.Infrastructure.Integrations.Email;
using TeacherAppointment.Infrastructure.Integrations.QrCodes;
using TeacherAppointment.Infrastructure.Persistence;
using TeacherAppointment.Infrastructure.Realtime;

namespace TeacherAppointment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SqliteOptions>()
            .Bind(configuration.GetSection(SqliteOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddHostedService<SqliteDbInitializer>();
        services.AddSignalR();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.AddSingleton<IQrCodeGenerator, PlaceholderQrCodeGenerator>();
        services.AddSingleton<IAuthChallengeNotifier, SignalRAuthChallengeNotifier>();
        services.AddScoped<ITeacherRepository, SqliteTeacherRepository>();
        services.AddScoped<IAuthChallengeRepository, SqliteAuthChallengeRepository>();
        services.AddScoped<IAuditLogRepository, SqliteAuditLogRepository>();
        services.AddScoped<IRefreshTokenRepository, SqliteRefreshTokenRepository>();

        return services;
    }
}
