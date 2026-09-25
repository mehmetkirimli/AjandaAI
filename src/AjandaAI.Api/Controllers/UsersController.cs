// User kaynağı için CRUD HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.
// HTTP status kodunu ApiResponseFilter, ResultType'a göre belirler.

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
    public Task<ApiResponse<PagedResult<UserListDto>>> GetAllAsync([FromQuery] UserFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetAllAsync(filter, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ApiResponse<UserDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<ApiResponse<UserDetailDto>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ApiResponse<UserDetailDto>> UpdateAsync(int id, UserUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    public Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
