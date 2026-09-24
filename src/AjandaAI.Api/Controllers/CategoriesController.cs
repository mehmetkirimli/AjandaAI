// Category lookup tablosu için HTTP uç noktalarıdır (okuma + yazma).
// DELETE gerçek silme yapmaz, kategoriyi pasife alır (IsActive = false).
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.

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
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryListDto>>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryListDto>>.Ok(categories));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryListDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _service.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound(NotFoundResponse(id));
        }

        return Ok(ApiResponse<CategoryListDto>.Ok(category));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CategoryListDto>>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(dto, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryListDto>>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _service.UpdateAsync(id, dto, cancellationToken);
        if (response is null)
        {
            return NotFound(NotFoundResponse(id));
        }

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryListDto>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var response = await _service.DeactivateAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound(NotFoundResponse(id));
        }

        return Ok(response);
    }

    private static ApiResponse<CategoryListDto> NotFoundResponse(int id) =>
        ApiResponse<CategoryListDto>.Fail($"Category {id} bulunamadı.");
}
