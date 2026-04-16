namespace TeacherAppointment.Application.Abstractions.Security;

public interface ISensitiveDataMaskingPolicy
{
    string MaskIdNo(string idNo);

    string? MaskEmail(string? email);
}
