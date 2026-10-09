// Category lookup tablosu için HTTP uç noktalarıdır (okuma + yazma).
// Okuma her kimlikli kullanıcıya açık; yazma yalnızca Admin rolüne (ADR 0018 Karar 6).
// DELETE gerçek silme yapmaz, kategoriyi pasife alır (IsActive = false).
// Yanıtlar ApiResponse<T> zarfında döner; status kodunu ApiResponseFilter belirler.

using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjandaAI.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ApiResponse<PagedResult<CategoryListDto>>> GetAllAsync([FromQuery] CategoryFilterDto filter, CancellationToken cancellationToken) =>
        _service.GetAllAsync(filter, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ApiResponse<CategoryListDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public Task<ApiResponse<CategoryListDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}")]
    public Task<ApiResponse<CategoryListDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    public Task<ApiResponse<CategoryListDto>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeactivateAsync(id, cancellationToken);
}
