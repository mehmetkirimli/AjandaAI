// YALNIZCA testlerde, AuthApiFactory üzerinden uygulamaya eklenen yardımcı controller.
// Henüz token isteyen gerçek bir endpoint olmadığı için AUTH-31/32/40 (korumalı endpoint) ve
// AUTH-49 (kimliksiz ICurrentUser.UserId -> 500) bu endpoint'lerle doğrulanır.

using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.IntegrationTests.Auth;

[ApiController]
[Route("test-probe")]
public class ProbeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public ProbeController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    // [AllowAnonymous] yok: FallbackPolicy bu endpoint'i kilitler.
    [HttpGet("protected")]
    public ApiResponse<int> Protected() => ApiResponse<int>.Ok(_currentUser.UserId);

    // Bilerek anonim: kimliksiz istek Application'a ulaşırsa UserId okumak patlar (ADR 0019).
    [AllowAnonymous]
    [HttpGet("anonymous-current-user")]
    public ApiResponse<int> AnonymousCurrentUser() => ApiResponse<int>.Ok(_currentUser.UserId);
}
