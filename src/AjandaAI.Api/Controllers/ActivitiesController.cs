// Activity kaynağı için okuma ve yazma (POST/PUT/DELETE) HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.
// HTTP status kodunu ApiResponseFilter, ResultType'a göre belirler.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

// GEÇİCİ: P4'te kaldırılacak (ADR 0019). Bu controller henüz token bilmiyor; AUTH-48 kalanı yakalar.
[AllowAnonymous]
[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly ActivityService _service;

    public ActivitiesController(ActivityService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ApiResponse<PagedResult<ActivityListDto>>> GetAllAsync([FromQuery] ActivityFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetAllAsync(filter, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ApiResponse<ActivityDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<ApiResponse<ActivityDetailDto>> CreateAsync(ActivityCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ApiResponse<ActivityDetailDto>> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    public Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
