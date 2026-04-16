using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Infrastructure.Security;

public sealed class DefaultSensitiveDataMaskingPolicy : ISensitiveDataMaskingPolicy
{
    public string MaskIdNo(string idNo)
    {
        if (string.IsNullOrWhiteSpace(idNo) || idNo.Length < 4)
        {
            return "****";
        }

        return $"{idNo[0]}{idNo[1]}*****{idNo[^2]}{idNo[^1]}";
    }

    public string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var parts = email.Split('@', 2);
        if (parts.Length != 2)
        {
            return "***";
        }

        var user = parts[0];
        var maskedUser = user.Length switch
        {
            <= 1 => "*",
            2 => $"{user[0]}*",
            _ => $"{user[0]}***{user[^1]}"
        };

        return $"{maskedUser}@{parts[1]}";
    }
}
