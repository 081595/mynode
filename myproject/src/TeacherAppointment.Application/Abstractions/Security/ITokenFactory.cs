using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Application.Abstractions.Security;

public interface ITokenFactory
{
    TokenEnvelope CreateAccessToken(TeacherIdentityRecord teacher, DateTime issuedAtUtc, DateTime expiresAtUtc);
}

public sealed record TokenEnvelope(string Value, DateTime ExpiresAtUtc);
