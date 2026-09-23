// Category lookup tablosu için salt-okunur HTTP uç noktalarıdır.
// Yanıtlar her zaman ApiResponse<T> zarfı içinde DTO olarak döner.

using AjandaAI.Application.Catalog;
using AjandaAI.Application.Catalog.Dtos;
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
            return NotFound(ApiResponse<CategoryListDto>.Fail($"Category {id} bulunamadı."));
        }

        return Ok(ApiResponse<CategoryListDto>.Ok(category));
    }
}
