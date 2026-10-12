// Activity kaynağı için okuma ve yazma (POST/PUT/DELETE) HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.
// HTTP status kodunu ApiResponseFilter, ResultType'a göre belirler.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

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

    [HttpPost("series")]
    public Task<ApiResponse<ActivitySeriesCreatedDto>> CreateSeriesAsync(ActivitySeriesCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateSeriesAsync(dto, cancellationToken);

    [HttpPost("series/{seriesId:int}/deactivate")]
    public Task<ApiResponse<SeriesDeactivatedDto>> DeactivateSeriesAsync(int seriesId, [FromQuery] DateOnly? from, CancellationToken cancellationToken) =>
        _service.DeactivateSeriesFromAsync(seriesId, from, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ApiResponse<ActivityDetailDto>> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    public Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
