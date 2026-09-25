// Reminder kayıtları için okuma ve yazma (POST/PUT/DELETE) HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.
// HTTP status kodunu ApiResponseFilter, ResultType'a göre belirler.

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
    public Task<ApiResponse<IReadOnlyList<ReminderListDto>>> GetAllAsync(CancellationToken cancellationToken) =>
        _service.GetAllAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<ReminderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status400BadRequest)]
    public Task<ApiResponse<ReminderDetailDto>> CreateAsync(ReminderCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReminderDetailDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<ReminderDetailDto>> UpdateAsync(int id, ReminderUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
