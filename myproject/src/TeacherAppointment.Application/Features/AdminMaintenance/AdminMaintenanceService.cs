namespace TeacherAppointment.Application.Features.AdminMaintenance;

public sealed class AdminMaintenanceService : IAdminMaintenanceService
{
    public Task<bool> IsAuthorizedAsync(string role, CancellationToken cancellationToken = default)
    {
        var allowed = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(allowed);
    }
}
