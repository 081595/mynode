using Microsoft.AspNetCore.Mvc;
using TeacherAppointment.Application.Abstractions.Infrastructure;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentityChallengeService _identityChallengeService;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public AuthController(IIdentityChallengeService identityChallengeService, IQrCodeGenerator qrCodeGenerator)
    {
        _identityChallengeService = identityChallengeService;
        _qrCodeGenerator = qrCodeGenerator;
    }

    [HttpPost("identify")]
    public async Task<ActionResult<IdentifyResponse>> IdentifyAsync([FromBody] IdentifyRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.InitializeAsync(request.IdNo, request.Birthday, cancellationToken);
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

    [HttpPost("verify-email")]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.VerifyEmailCodeAsync(request.ChallengeId, request.Code, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        return Ok(new VerifyEmailResponse(result.ChallengeId, result.IsVerified, result.VerifiedAtUtc));
    }

    [HttpPost("qr-sessions")]
    public async Task<ActionResult<CreateQrSessionResponse>> CreateQrSessionAsync([FromBody] CreateQrSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.CreateQrSessionAsync(request.ChallengeId, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.SessionId) || result.ExpiresAtUtc is null)
        {
            return BadRequest(new ErrorResponse(result.Message));
        }

        var confirmationPayload = $"teacher-appointment://auth/qr-confirm?sessionId={result.SessionId}";
        var qrCodeDataUri = _qrCodeGenerator.GenerateDataUri(confirmationPayload);

        return Ok(new CreateQrSessionResponse(result.ChallengeId!, result.SessionId, result.ExpiresAtUtc.Value, confirmationPayload, qrCodeDataUri));
    }

    [HttpPost("qr-sessions/{sessionId}/confirm")]
    public async Task<ActionResult<ConfirmQrSessionResponse>> ConfirmQrSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.ConfirmQrSessionAsync(sessionId, cancellationToken);
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

    public sealed record IdentifyRequest(string IdNo, DateOnly Birthday);

    public sealed record IdentifyResponse(
        string ChallengeId,
        string? EmployeeNo,
        string? MaskedEmail,
        DateTime ExpiresAtUtc,
        DateTime ResendAvailableAtUtc,
        bool EmailDeliveryAvailable);

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

    public sealed record ErrorResponse(string Message);
}
