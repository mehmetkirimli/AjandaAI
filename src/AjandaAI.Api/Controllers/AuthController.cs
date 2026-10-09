// Kimlik doğrulama uç noktaları: register, verify-email, login, refresh, logout (ADR 0018).
// Hepsi herkese açıktır ([AllowAnonymous]; FallbackPolicy varsayılanı kilitler, ADR 0019).
// Register ve login IP bazlı rate limit alır. Yanıtlar ApiResponse<T>; status kodunu ApiResponseFilter belirler.

using AjandaAI.Api.Auth;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AjandaAI.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;

    public AuthController(AuthService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public Task<ApiResponse<object>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken) =>
        _service.RegisterAsync(dto, cancellationToken);

    [HttpPost("verify-email")]
    public Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken) =>
        _service.VerifyEmailAsync(dto, cancellationToken);

    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken) =>
        _service.LoginAsync(dto, cancellationToken);

    [HttpPost("refresh")]
    public Task<ApiResponse<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken) =>
        _service.RefreshAsync(dto, cancellationToken);

    [HttpPost("logout")]
    public Task<ApiResponse<bool>> LogoutAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken) =>
        _service.LogoutAsync(dto, cancellationToken);
}
