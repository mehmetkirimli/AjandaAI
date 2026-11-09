// Kimlik doğrulama uç noktaları: register, verify-email, login, refresh, logout (ADR 0018) ve profil: me, change-password (ADR 0021).
// me ve change-password dışındakiler herkese açıktır. [AllowAnonymous] action seviyesindedir: sınıf seviyesinde olsaydı
// me'ye yazılan koruma etkisiz kalırdı (AllowAnonymous her Authorize'ı ezer). me FallbackPolicy ile korunur.
// Register ve login IP bazlı rate limit alır. Yanıtlar ApiResponse<T>; status kodunu ApiResponseFilter belirler.

using AjandaAI.Api.Auth;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;
    private readonly ProfileService _profile;
    private readonly PersonalAccessTokenService _pats;

    public AuthController(AuthService service, ProfileService profile, PersonalAccessTokenService pats)
    {
        _service = service;
        _profile = profile;
        _pats = pats;
    }

    // Kişisel erişim anahtarları (ADR 0024 §1). Normal JWT oturumu gerekir; PAT ile çağrılamaz (MCP-05).
    [Authorize]
    [HttpGet("tokens")]
    public Task<ApiResponse<List<PersonalAccessTokenDto>>> ListTokensAsync(CancellationToken cancellationToken) =>
        _pats.ListAsync(cancellationToken);

    [Authorize]
    [HttpPost("tokens")]
    public Task<ApiResponse<PersonalAccessTokenCreatedDto>> CreateTokenAsync(PersonalAccessTokenCreateDto dto, CancellationToken cancellationToken) =>
        _pats.CreateAsync(dto, cancellationToken);

    [Authorize]
    [HttpDelete("tokens/{id:int}")]
    public Task<ApiResponse<bool>> RevokeTokenAsync(int id, CancellationToken cancellationToken) =>
        _pats.RevokeAsync(id, cancellationToken);

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public Task<ApiResponse<object>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken) =>
        _service.RegisterAsync(dto, cancellationToken);

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken) =>
        _service.VerifyEmailAsync(dto, cancellationToken);

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken) =>
        _service.LoginAsync(dto, cancellationToken);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<ApiResponse<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken) =>
        _service.RefreshAsync(dto, cancellationToken);

    [AllowAnonymous]
    [HttpPost("logout")]
    public Task<ApiResponse<bool>> LogoutAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken) =>
        _service.LogoutAsync(dto, cancellationToken);

    [HttpGet("me")]
    public Task<ApiResponse<MeDto>> MeAsync(CancellationToken cancellationToken) =>
        _profile.GetAsync(cancellationToken);

    [HttpPut("me")]
    public Task<ApiResponse<MeDto>> UpdateMeAsync(ProfileUpdateDto dto, CancellationToken cancellationToken) =>
        _profile.UpdateAsync(dto, cancellationToken);

    // Mevcut şifre tahminine karşı login ile aynı IP sınırı.
    [HttpPost("change-password")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken) =>
        _profile.ChangePasswordAsync(dto, cancellationToken);
}
