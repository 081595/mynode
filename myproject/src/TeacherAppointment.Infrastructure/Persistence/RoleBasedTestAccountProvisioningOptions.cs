using System.ComponentModel.DataAnnotations;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class RoleBasedTestAccountProvisioningOptions
{
    public const string SectionName = "RoleBasedTestAccountProvisioning";

    public bool Enabled { get; init; }

    [MinLength(1)]
    public string[] EligibleEnvironments { get; init; } = ["Development", "Testing", "Staging"];
}