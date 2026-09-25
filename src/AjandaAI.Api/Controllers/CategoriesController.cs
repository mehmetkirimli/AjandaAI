// Category lookup tablosu için HTTP uç noktalarıdır (okuma + yazma).
// DELETE gerçek silme yapmaz, kategoriyi pasife alır (IsActive = false).
// Yanıtlar ApiResponse<T> zarfında döner; status kodunu ApiResponseFilter belirler.

using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
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
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryListDto>>), StatusCodes.Status200OK)]
    public Task<ApiResponse<IReadOnlyList<CategoryListDto>>> GetAllAsync(CancellationToken cancellationToken) =>
        _service.GetAllAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<CategoryListDto>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status400BadRequest)]
    public Task<ApiResponse<CategoryListDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken) =>
        _service.CreateAsync(dto, cancellationToken);

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<CategoryListDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public Task<ApiResponse<CategoryListDto>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _service.DeactivateAsync(id, cancellationToken);
}
