// Reminder kayıtları için okuma ve yazma (POST/PUT/DELETE) HTTP uç noktalarıdır.
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

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ReminderDetailDto>>> CreateAsync(ReminderCreateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(dto, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReminderDetailDto>>> UpdateAsync(int id, ReminderUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.UpdateAsync(id, dto, cancellationToken);
        if (response is null)
        {
            return NotFound(ApiResponse<ReminderDetailDto>.Fail($"Reminder {id} bulunamadı."));
        }

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _service.DeleteAsync(id, cancellationToken))
        {
            return NotFound(ApiResponse<bool>.Fail($"Reminder {id} bulunamadı."));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Reminder silindi."));
    }
}
