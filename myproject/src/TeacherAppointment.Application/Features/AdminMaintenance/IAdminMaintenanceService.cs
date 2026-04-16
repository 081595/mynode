namespace TeacherAppointment.Application.Features.AdminMaintenance;

public interface IAdminMaintenanceService
{
    Task<bool> IsAuthorizedAsync(string role, CancellationToken cancellationToken = default);
}
