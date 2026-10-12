// Category okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Liste yalnızca aktifleri döner (Admin includeInactive=true ile pasifleri de görür, ADMIN-11); Id ile sorgu pasif kaydı da döner.
// Silme gerçek silme değildir: IsActive = false yapılır (lookup table).
// Kayıt yoksa ApiResponse.NotFound döner; HTTP status kodunu ApiResponseFilter belirler.

using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Categories.Validators;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Categories;

public class CategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IValidator<CategoryCreateDto> _createValidator;
    private readonly IValidator<CategoryUpdateDto> _updateValidator;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository repository,
        IValidator<CategoryCreateDto> createValidator,
        IValidator<CategoryUpdateDto> updateValidator,
        ICurrentUser currentUser,
        ILogger<CategoryService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<CategoryListDto>>> GetAllAsync(CategoryFilterDto filter, CancellationToken cancellationToken = default)
    {
        // ADMIN-11: User rolünde parametre sessizce yok sayılır. IsAdmin yalnızca istendiğinde okunur.
        if (filter.IncludeInactive && !_currentUser.IsAdmin)
            filter.IncludeInactive = false;
        var page = await _repository.GetPagedAsync(filter, cancellationToken);
        return ApiResponse<PagedResult<CategoryListDto>>.Ok(page.Map(ToDto));
    }

    public async Task<ApiResponse<CategoryListDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        return category is null ? NotFound(id) : ApiResponse<CategoryListDto>.Ok(ToDto(category));
    }

    public async Task<ApiResponse<CategoryListDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Kategori oluşturma doğrulama hatası {@Errors}", errors);
            return ApiResponse<CategoryListDto>.Fail("Doğrulama hatası.", errors);
        }

        var category = new Category
        {
            Name = dto.Name.Trim(),
            // ADR 0023 Karar 3: yeni kategori en sona eklenir.
            SortOrder = await _repository.GetMaxSortOrderAsync(cancellationToken) + 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(category, cancellationToken);
        _logger.LogInformation("Kategori oluşturuldu {CategoryId} {Name}", category.Id, category.Name);
        return ApiResponse<CategoryListDto>.Created(ToDto(category), "Kategori oluşturuldu.");
    }

    public async Task<ApiResponse<CategoryListDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(id);

        var context = new ValidationContext<CategoryUpdateDto>(dto);
        context.RootContextData[CategoryUpdateDtoValidator.IdKey] = id;
        var result = await _updateValidator.ValidateAsync(context, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Kategori güncelleme doğrulama hatası {CategoryId} {@Errors}", id, errors);
            return ApiResponse<CategoryListDto>.Fail("Doğrulama hatası.", errors);
        }

        category.Name = dto.Name.Trim();
        category.IsActive = dto.IsActive;
        await _repository.UpdateAsync(category, cancellationToken);
        _logger.LogInformation("Kategori güncellendi {CategoryId} {Name}", id, category.Name);
        return ApiResponse<CategoryListDto>.Ok(ToDto(category), "Kategori güncellendi.");
    }

    public async Task<ApiResponse<CategoryListDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound(id);

        if (category.IsActive)
        {
            category.IsActive = false;
            await _repository.UpdateAsync(category, cancellationToken);
            _logger.LogInformation("Kategori pasife alındı {CategoryId}", id);
        }
        return ApiResponse<CategoryListDto>.Ok(ToDto(category), "Kategori pasife alındı.");
    }

    // ADR 0023 Karar 2: liste mevcut tüm Id kümesiyle birebir aynı olmalı; değilse hiçbir şey değişmez.
    public async Task<ApiResponse<object>> ReorderAsync(CategoryOrderDto dto, CancellationToken cancellationToken = default)
    {
        var ids = dto.CategoryIds ?? Array.Empty<int>();
        var existing = await _repository.GetAllIdsAsync(cancellationToken);
        var errors = new List<string>();

        if (ids.Distinct().Count() != ids.Length)
            errors.Add("Sıralama listesinde tekrarlı kategori var.");
        var unknown = ids.Except(existing).ToList();
        if (unknown.Count > 0)
            errors.Add($"Bilinmeyen kategori: {string.Join(", ", unknown)}.");
        var missing = existing.Except(ids).ToList();
        if (missing.Count > 0)
            errors.Add($"Sıralama listesinde eksik kategori: {string.Join(", ", missing)}.");

        if (errors.Count > 0)
        {
            _logger.LogWarning("Kategori sıralama doğrulama hatası {@Errors}", errors);
            return ApiResponse<object>.Fail("Sıralama listesi tüm kategorileri birer kez içermelidir.", errors);
        }

        await _repository.ReorderAsync(ids, cancellationToken);
        _logger.LogInformation("Kategori sırası güncellendi {Count}", ids.Length);
        return ApiResponse<object>.NoContent("Kategori sırası güncellendi.");
    }

    private static ApiResponse<CategoryListDto> NotFound(int id) =>
        ApiResponse<CategoryListDto>.NotFound($"Category {id} bulunamadı.");

    private static CategoryListDto ToDto(Category c) => new(c.Id, c.Name, c.IsActive, c.SortOrder);
}
