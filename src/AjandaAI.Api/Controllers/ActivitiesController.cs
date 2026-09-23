// Activity kaynağı için salt-okunur HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.

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
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityListDto>>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var activities = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ActivityListDto>>.Ok(activities));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityDetailDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var activity = await _service.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return NotFound(ApiResponse<ActivityDetailDto>.Fail($"Activity {id} bulunamadı."));
        }

        return Ok(ApiResponse<ActivityDetailDto>.Ok(activity));
    }
}
