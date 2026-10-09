// Admin kullanıcı yönetimi uçları (ADR 0018 Karar 6). Yalnızca Admin rolü; User rolü 403 (AUTH-50).
// DELETE gerçek silme yapmaz, kullanıcıyı pasife alır ve oturumlarını kapatır.

using AjandaAI.Application.Admin;
using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly AdminUserService _service;

    public AdminUsersController(AdminUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ApiResponse<PagedResult<AdminUserDto>>> GetAllAsync([FromQuery] AdminUserFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetAllAsync(filter, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ApiResponse<AdminUserDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<ApiResponse<AdminUserDto>> CreateAsync(AdminUserCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ApiResponse<AdminUserDto>> UpdateAsync(int id, AdminUserUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    public Task<ApiResponse<AdminUserDto>> DeactivateAsync(int id, CancellationToken cancellationToken) =>
        _service.DeactivateAsync(id, cancellationToken);
}
