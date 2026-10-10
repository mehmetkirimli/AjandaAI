// Admin aktivite/hatırlatma uçları (ADR 0018 Karar 6): yalnızca üstveri okuma ve aktivite moderasyonu.
// İçerik (Title, Description, Location, Note) yanıtlarda yoktur (AUTH-51). Yalnızca Admin rolü (AUTH-50).

using AjandaAI.Application.Admin;
using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin")]
public class AdminContentController : ControllerBase
{
    private readonly AdminContentService _service;

    public AdminContentController(AdminContentService service)
    {
        _service = service;
    }

    [HttpGet("activities")]
    public Task<ApiResponse<PagedResult<AdminActivityDto>>> GetActivitiesAsync([FromQuery] AdminActivityFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetActivitiesAsync(filter, cancellationToken);

    [HttpGet("activities/{id:int}")]
    public Task<ApiResponse<AdminActivityDto>> GetActivityAsync(int id, CancellationToken cancellationToken) =>
        _service.GetActivityAsync(id, cancellationToken);

    [HttpDelete("activities/{id:int}")]
    public Task<ApiResponse<AdminActivityDto>> DeactivateActivityAsync(int id, CancellationToken cancellationToken) =>
        _service.DeactivateActivityAsync(id, cancellationToken);

    [HttpGet("reminders")]
    public Task<ApiResponse<PagedResult<AdminReminderDto>>> GetRemindersAsync([FromQuery] AdminReminderFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetRemindersAsync(filter, cancellationToken);
}
