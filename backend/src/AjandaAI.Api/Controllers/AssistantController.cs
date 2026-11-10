// Uygulama içi asistan ucu (ADR 0025 §4). Yalnızca normal oturum (JWT) kabul edilir; kişisel erişim anahtarı
// (ajd_...) varsayılan şemada geçmez, 401 alır (ASSIST-02, 03). Kullanıcı başına istek sınırı vardır (ASSIST-22, 23).
// Asistan veritabanına yazmaz; yanıttaki öneriler (proposals) panelde onaylanınca mevcut uçlara gönderilir.

using AjandaAI.Api.Assistant;
using AjandaAI.Application.Assistant;
using AjandaAI.Application.Assistant.Dtos;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly AssistantService _service;

    public AssistantController(AssistantService service)
    {
        _service = service;
    }

    [EnableRateLimiting(AssistantRateLimit.PolicyName)]
    [HttpPost("chat")]
    public Task<ApiResponse<AssistantChatResponseDto>> ChatAsync(AssistantChatRequestDto dto, CancellationToken cancellationToken) =>
        _service.ChatAsync(dto, cancellationToken);
}
