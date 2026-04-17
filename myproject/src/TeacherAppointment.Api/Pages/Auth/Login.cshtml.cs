using Microsoft.AspNetCore.Mvc;
using TeacherAppointment.Application.Features.Auth;

namespace TeacherAppointment.Api.Pages.Auth;

public sealed class LoginModel : PortalPageModel
{
    private readonly IIdentityChallengeService _identityChallengeService;

    public LoginModel(IIdentityChallengeService identityChallengeService)
    {
        _identityChallengeService = identityChallengeService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string IdNo { get; set; } = string.Empty;
        public DateOnly Birthday { get; set; }
    }

    public IActionResult OnGet()
    {
        if (IsAuthenticated)
        {
            return RedirectToPage("/Teacher/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _identityChallengeService.InitializeAsync(
            Input.IdNo,
            Input.Birthday,
            BuildClientContext(),
            cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.ChallengeId))
        {
            ViewData["InlineError"] = "身分驗證失敗，請確認輸入後重試。";
            return Page();
        }

        return RedirectToPage("/Auth/Verify", new { challengeId = result.ChallengeId });
    }

    private AuthClientContext BuildClientContext()
    {
        return new AuthClientContext(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());
    }
}
