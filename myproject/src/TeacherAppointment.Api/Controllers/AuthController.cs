using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TeacherAppointment.Api.Security;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentityChallengeService _identityChallengeService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly AuthCookieOptions _cookieOptions;
    private readonly QrOptions _qrOptions;

    public AuthController(
        IIdentityChallengeService identityChallengeService,
        IAuthSessionService authSessionService,
        IQrCodeGenerator qrCodeGenerator,
        IOptions<AuthCookieOptions> cookieOptions,
        IOptions<QrOptions> qrOptions)
    {
        _identityChallengeService = identityChallengeService;
        _authSessionService = authSessionService;
        _qrCodeGenerator = qrCodeGenerator;
        _cookieOptions = cookieOptions.Value;
        _qrOptions = qrOptions.Value;
    }

    [HttpPost("identify")]
    public async Task<ActionResult<IdentifyResponse>> IdentifyAsync([FromBody] IdentifyRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.InitializeAsync(request.IdNo, request.Birthday, BuildClientContext(), cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ChallengeId))
        {
            return Unauthorized(new ErrorResponse(result.Message));
        }

        return Ok(new IdentifyResponse(
            result.ChallengeId,
            result.EmployeeNo,
            result.MaskedEmail,
            result.ExpiresAtUtc!.Value,
            result.ResendAvailableAtUtc!.Value,
            !string.IsNullOrWhiteSpace(result.MaskedEmail)));
    }

    [HttpPost("challenges/{challengeId}/resend")]
    public async Task<ActionResult<ResendChallengeResponse>> ResendChallengeAsync(string challengeId, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.ResendChallengeAsync(challengeId, BuildClientContext(), cancellationToken);
        if (!result.Success)
        {
            if (result.Throttled)
            {
                if (result.RetryAfter is not null)
                {
                    Response.Headers["Retry-After"] = Math.Ceiling(result.RetryAfter.Value.TotalSeconds).ToString("0");
                }

                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse(result.Message));
            }

            return BadRequest(new ErrorResponse(result.Message));
        }

        return Ok(new ResendChallengeResponse(
            result.ChallengeId,
            result.ExpiresAtUtc!.Value,
            result.ResendAvailableAtUtc!.Value));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.VerifyEmailCodeAsync(request.ChallengeId, request.Code, BuildClientContext(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        return Ok(new VerifyEmailResponse(result.ChallengeId, result.IsVerified, result.VerifiedAtUtc));
    }

    [HttpPost("qr-sessions")]
    public async Task<ActionResult<CreateQrSessionResponse>> CreateQrSessionAsync([FromBody] CreateQrSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.CreateQrSessionAsync(request.ChallengeId, BuildClientContext(), cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.SessionId) || result.ExpiresAtUtc is null)
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        var confirmationPayload = BuildQrConfirmationUrl(result.SessionId);
        var qrCodeDataUri = _qrCodeGenerator.GenerateDataUri(confirmationPayload);

        return Ok(new CreateQrSessionResponse(result.ChallengeId!, result.SessionId, result.ExpiresAtUtc.Value, confirmationPayload, qrCodeDataUri));
    }

    [HttpGet("qr-sessions/{sessionId}/confirm")]
    public IActionResult ConfirmQrSessionByGet(string sessionId)
    {
        // 導向 Razor Page 讓使用者手動按下驗證按鈕
        return Redirect($"/auth/confirm?sessionId={Uri.EscapeDataString(sessionId)}");
    }

    [HttpPost("qr-sessions/{sessionId}/confirm")]
    public async Task<ActionResult<ConfirmQrSessionResponse>> ConfirmQrSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.ConfirmQrSessionAsync(sessionId, BuildClientContext(), cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ChallengeId))
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        return Ok(new ConfirmQrSessionResponse(
            result.ChallengeId,
            result.SessionId!,
            result.IsVerified,
            result.VerifiedAtUtc,
            result.RedirectUrl));
    }

    [HttpPost("sessions/exchange")]
    public async Task<ActionResult<SessionResponse>> ExchangeSessionAsync([FromBody] ExchangeSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _authSessionService.IssueTokensAsync(request.ChallengeId, BuildClientContext(), cancellationToken);
        if (!result.Success ||
            string.IsNullOrWhiteSpace(result.AccessToken) ||
            string.IsNullOrWhiteSpace(result.RefreshToken) ||
            result.AccessTokenExpiresAtUtc is null ||
            result.RefreshTokenExpiresAtUtc is null)
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        WriteAuthCookies(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc.Value,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc.Value);

        return Ok(new SessionResponse(
            result.EmployeeNo,
            result.AccessTokenExpiresAtUtc.Value,
            result.RefreshTokenExpiresAtUtc.Value));
    }

    [HttpPost("sessions/refresh")]
    public async Task<ActionResult<SessionResponse>> RefreshSessionAsync(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(_cookieOptions.RefreshTokenName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new ErrorResponse("Missing refresh token."));
        }

        var result = await _authSessionService.RefreshAsync(refreshToken, BuildClientContext(), cancellationToken);
        if (!result.Success ||
            string.IsNullOrWhiteSpace(result.AccessToken) ||
            string.IsNullOrWhiteSpace(result.RefreshToken) ||
            result.AccessTokenExpiresAtUtc is null ||
            result.RefreshTokenExpiresAtUtc is null)
        {
            return Unauthorized(new ErrorResponse(result.Message));
        }

        WriteAuthCookies(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc.Value,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc.Value);

        return Ok(new SessionResponse(
            result.EmployeeNo,
            result.AccessTokenExpiresAtUtc.Value,
            result.RefreshTokenExpiresAtUtc.Value));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<LogoutResponse>> LogoutAsync(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(_cookieOptions.RefreshTokenName, out var refreshToken);
        var result = await _authSessionService.LogoutAsync(refreshToken, BuildClientContext(), cancellationToken);

        DeleteAuthCookies();

        return Ok(new LogoutResponse(result.Success, result.Message));
    }

    private void WriteAuthCookies(string accessToken, DateTime accessExpiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
    {
        var sameSite = ParseSameSite(_cookieOptions.SameSite);

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

    private static SameSiteMode ParseSameSite(string value)
    {
        return Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : SameSiteMode.Strict;
    }

    private string BuildQrConfirmationUrl(string sessionId)
    {
        string baseUrl = null;
        if (!string.IsNullOrWhiteSpace(_qrOptions.PublicBaseUrl) &&
            Uri.TryCreate(_qrOptions.PublicBaseUrl, UriKind.Absolute, out var baseUri))
        {
            baseUrl = baseUri.ToString().TrimEnd('/');
        }
        else if (ResolveCodespacesPublicBaseUrl() is string codespacesBaseUrl)
        {
            baseUrl = codespacesBaseUrl.TrimEnd('/');
        }
        else
        {
            baseUrl = $"{Request.Scheme}://{Request.Host}";
        }

        var escapedSessionId = Uri.EscapeDataString(sessionId);
        // 產生 /auth/confirm?sessionId=xxx 連結
        return $"{baseUrl}/auth/confirm?sessionId={escapedSessionId}";
    }

    private string? ResolveCodespacesPublicBaseUrl()
    {
        var codespaceName = Environment.GetEnvironmentVariable("CODESPACE_NAME");
        var forwardingDomain = Environment.GetEnvironmentVariable("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN");
        if (string.IsNullOrWhiteSpace(codespaceName) || string.IsNullOrWhiteSpace(forwardingDomain))
        {
            return null;
        }

        var port = Request.Host.Port
            ?? (string.Equals(Request.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

        return $"https://{codespaceName}-{port}.{forwardingDomain}";
    }

    private void DeleteAuthCookies()
    {
        var deleteOptions = new CookieOptions
        {
            HttpOnly = _cookieOptions.HttpOnly,
            Secure = _cookieOptions.Secure,
            SameSite = ParseSameSite(_cookieOptions.SameSite),
            Path = "/"
        };

        Response.Cookies.Delete(_cookieOptions.AccessTokenName, deleteOptions);
        Response.Cookies.Delete(_cookieOptions.RefreshTokenName, deleteOptions);
    }

    private AuthClientContext BuildClientContext()
    {
        return new AuthClientContext(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());
    }

    public sealed record IdentifyRequest(string IdNo, DateOnly Birthday);

    public sealed record IdentifyResponse(
        string ChallengeId,
        string? EmployeeNo,
        string? MaskedEmail,
        DateTime ExpiresAtUtc,
        DateTime ResendAvailableAtUtc,
        bool EmailDeliveryAvailable);

    public sealed record ResendChallengeResponse(
        string ChallengeId,
        DateTime ExpiresAtUtc,
        DateTime ResendAvailableAtUtc);

    public sealed record VerifyEmailRequest(string ChallengeId, string Code);

    public sealed record VerifyEmailResponse(string ChallengeId, bool IsVerified, DateTime? VerifiedAtUtc);

    public sealed record CreateQrSessionRequest(string ChallengeId);

    public sealed record CreateQrSessionResponse(
        string ChallengeId,
        string SessionId,
        DateTime ExpiresAtUtc,
        string ConfirmationPayload,
        string QrCodeDataUri);

    public sealed record ConfirmQrSessionResponse(
        string ChallengeId,
        string SessionId,
        bool IsVerified,
        DateTime? VerifiedAtUtc,
        string? RedirectUrl);

    public sealed record ExchangeSessionRequest(string ChallengeId);

    public sealed record SessionResponse(string? EmployeeNo, DateTime AccessTokenExpiresAtUtc, DateTime RefreshTokenExpiresAtUtc);

    public sealed record LogoutResponse(bool Success, string Message);

    public sealed record ErrorResponse(string Message);
}
