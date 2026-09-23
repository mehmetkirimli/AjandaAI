// Reminder kayıtları için salt-okunur HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.

using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders;
using AjandaAI.Application.Reminders.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController : ControllerBase
{
    private readonly ReminderService _service;

    public RemindersController(ReminderService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReminderListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReminderListDto>>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var reminders = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReminderListDto>>.Ok(reminders));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReminderDetailDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var reminder = await _service.GetByIdAsync(id, cancellationToken);
        if (reminder is null)
        {
            return NotFound(ApiResponse<ReminderDetailDto>.Fail($"Reminder {id} bulunamadı."));
        }

        return Ok(ApiResponse<ReminderDetailDto>.Ok(reminder));
    }
}
