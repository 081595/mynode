using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TeacherAppointment.Api.Security;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Api.Pages.Auth;

public sealed class VerifyModel : PortalPageModel
{
    private readonly IIdentityChallengeService _identityChallengeService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly AuthCookieOptions _cookieOptions;
    private readonly IAuthChallengeRepository _authChallengeRepository;
    private readonly IHostEnvironment _hostEnvironment;

    public VerifyModel(
        IIdentityChallengeService identityChallengeService,
        IAuthSessionService authSessionService,
        IQrCodeGenerator qrCodeGenerator,
        IOptions<AuthCookieOptions> cookieOptions,
        IAuthChallengeRepository authChallengeRepository,
        IHostEnvironment hostEnvironment)
    {
        _identityChallengeService = identityChallengeService;
        _authSessionService = authSessionService;
        _qrCodeGenerator = qrCodeGenerator;
        _cookieOptions = cookieOptions.Value;
        _authChallengeRepository = authChallengeRepository;
        _hostEnvironment = hostEnvironment;
    }

    [BindProperty(SupportsGet = true)]
    public string ChallengeId { get; set; } = string.Empty;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string? QrSessionId { get; private set; }

    public string? QrCodeDataUri { get; private set; }

    public DateTime? QrExpiresAtUtc { get; private set; }

    public bool EmailVerified { get; private set; }

    public string? DevVerificationCode { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ChallengeId))
        {
            FlashWarning = "缺少驗證挑戰資訊，請重新登入。";
            return RedirectToPage("/Auth/Login");
        }

        if (_hostEnvironment.IsDevelopment())
        {
            var challenge = await _authChallengeRepository.GetByIdAsync(ChallengeId, cancellationToken);
            DevVerificationCode = challenge?.VerificationCode;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostVerifyEmailAsync(CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.VerifyEmailCodeAsync(
            ChallengeId,
            Code,
            BuildClientContext(),
            cancellationToken);

        if (!result.Success)
        {
            return new JsonResult(new { success = false, message = result.Message });
        }

        EmailVerified = result.IsVerified;
        return new JsonResult(new { success = true, message = "Email 驗證成功，可完成登入。" });
    }

    public async Task<IActionResult> OnPostResendAsync(CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.ResendChallengeAsync(
            ChallengeId,
            BuildClientContext(),
            cancellationToken);

        if (!result.Success)
        {
            return new JsonResult(new { success = false, message = result.Message });
        }

        return new JsonResult(new { success = true, message = "驗證碼已重新寄送。" });
    }

    public async Task<IActionResult> OnPostCreateQrAsync(CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.CreateQrSessionAsync(
            ChallengeId,
            BuildClientContext(),
            cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.SessionId) || result.ExpiresAtUtc is null)
        {
            return new JsonResult(new { success = false, message = result.Message });
        }

        var payload = $"teacher-appointment://auth/qr-confirm?sessionId={result.SessionId}";
        var dataUri = _qrCodeGenerator.GenerateDataUri(payload);

        return new JsonResult(new
        {
            success = true,
            message = "QR 驗證已建立，請用手機掃描。",
            sessionId = result.SessionId,
            qrCodeDataUri = dataUri,
            expiresAtUtc = result.ExpiresAtUtc
        });
    }

    public async Task<IActionResult> OnPostExchangeAsync(CancellationToken cancellationToken)
    {
        var result = await _authSessionService.IssueTokensAsync(
            ChallengeId,
            BuildClientContext(),
            cancellationToken);

        if (!result.Success ||
            string.IsNullOrWhiteSpace(result.AccessToken) ||
            string.IsNullOrWhiteSpace(result.RefreshToken) ||
            result.AccessTokenExpiresAtUtc is null ||
            result.RefreshTokenExpiresAtUtc is null)
        {
            return new JsonResult(new { success = false, message = result.Message });
        }

        WriteAuthCookies(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc.Value,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc.Value);

        return new JsonResult(new { success = true, message = "登入成功，正在導向作業頁面。", redirectUrl = Url.Page("/Teacher/Index") });
    }

    public IActionResult OnGetPoll(string challengeId)
    {
        if (string.IsNullOrWhiteSpace(challengeId))
        {
            return new JsonResult(new { success = false, message = "challengeId missing" });
        }

        return new JsonResult(new { success = true, challengeId });
    }

    private void WriteAuthCookies(string accessToken, DateTime accessExpiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookieOptions.SameSite, ignoreCase: true, out var parsed)
            ? parsed
            : SameSiteMode.Strict;

        Response.Cookies.Append(
            _cookieOptions.AccessTokenName,
            accessToken,
            new CookieOptions
            {
                HttpOnly = _cookieOptions.HttpOnly,
                Secure = _cookieOptions.Secure,
                SameSite = sameSite,
                Expires = new DateTimeOffset(accessExpiresAtUtc)
            });

        Response.Cookies.Append(
            _cookieOptions.RefreshTokenName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = _cookieOptions.HttpOnly,
                Secure = _cookieOptions.Secure,
                SameSite = sameSite,
                Expires = new DateTimeOffset(refreshExpiresAtUtc)
            });
    }

    private AuthClientContext BuildClientContext()
    {
        return new AuthClientContext(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());
    }
}
