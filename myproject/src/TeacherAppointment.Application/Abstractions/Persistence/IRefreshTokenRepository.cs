namespace TeacherAppointment.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshTokenRecord token, CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord?> GetActiveAsync(string refreshToken, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    Task RevokeByUserAsync(int year, string employeeNo, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenRecord(
    string RefreshToken,
    int Year,
    string EmployeeNo,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc);
