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
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActivityListDto>>), StatusCodes.Status200OK)]
    public Task<ApiResponse<IReadOnlyList<ActivityListDto>>> GetAllAsync(CancellationToken cancellationToken) =>
        _service.GetAllAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<ActivityDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status400BadRequest)]
    public Task<ApiResponse<ActivityDetailDto>> CreateAsync(ActivityCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<ActivityDetailDto>> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
