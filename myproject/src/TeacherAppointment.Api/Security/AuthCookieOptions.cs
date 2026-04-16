using System.ComponentModel.DataAnnotations;

namespace TeacherAppointment.Api.Security;

public sealed class AuthCookieOptions
{
    public const string SectionName = "AuthCookies";
    public const string AccessTokenCookieName = "ta_access_token";
    public const string RefreshTokenCookieName = "ta_refresh_token";

    [Required]
    public string AccessTokenName { get; init; } = AccessTokenCookieName;

    [Required]
    public string RefreshTokenName { get; init; } = RefreshTokenCookieName;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;

    [Required]
    public string SameSite { get; init; } = "Strict";

    public bool HttpOnly { get; init; } = true;
    public bool Secure { get; init; } = true;
}
