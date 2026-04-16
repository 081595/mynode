namespace TeacherAppointment.Application.Features.AppointmentWorkflow;

using TeacherAppointment.Application.Abstractions.Persistence;

public interface IAppointmentQueryService
{
    Task<IReadOnlyList<AppointmentResponseSummary>> GetTeacherAppointmentsAsync(
        int year,
        string employeeNo,
        CancellationToken cancellationToken = default);
}
