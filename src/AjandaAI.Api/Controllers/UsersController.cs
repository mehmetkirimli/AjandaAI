// User kaynağı için CRUD HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.

using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using AjandaAI.Application.Users.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListDto>>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserListDto>>.Ok(users));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _service.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserDetailDto>.Fail(NotFoundMessage(id)));
        }

        return Ok(ApiResponse<UserDetailDto>.Ok(user));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(dto, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateAsync(int id, UserUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.UpdateAsync(id, dto, cancellationToken);
        if (response is null)
        {
            return NotFound(ApiResponse<UserDetailDto>.Fail(NotFoundMessage(id)));
        }

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var response = await _service.DeleteAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound(ApiResponse<bool>.Fail(NotFoundMessage(id)));
        }

        return Ok(response);
    }

    private static string NotFoundMessage(int id) => $"User {id} bulunamadı.";
}
