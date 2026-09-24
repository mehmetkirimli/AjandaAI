// Category okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Liste yalnızca aktifleri döner; Id ile sorgu pasif kaydı da döner.
// Silme gerçek silme değildir: IsActive = false yapılır (lookup table).
// Yazma metodları kayıt yoksa null döner; controller bunu 404'e çevirir.

using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Categories.Validators;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using FluentValidation;

namespace AjandaAI.Application.Categories;

public class CategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IValidator<CategoryCreateDto> _createValidator;
    private readonly IValidator<CategoryUpdateDto> _updateValidator;

    public CategoryService(
        ICategoryRepository repository,
        IValidator<CategoryCreateDto> createValidator,
        IValidator<CategoryUpdateDto> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CategoryListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetActiveAsync(cancellationToken);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryListDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        return category is null ? null : ToDto(category);
    }

    public async Task<ApiResponse<CategoryListDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
            return ApiResponse<CategoryListDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());

        var category = new Category
        {
            Name = dto.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(category, cancellationToken);
        return ApiResponse<CategoryListDto>.Ok(ToDto(category), "Kategori oluşturuldu.");
    }

    public async Task<ApiResponse<CategoryListDto>?> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return null;

        var context = new ValidationContext<CategoryUpdateDto>(dto);
        context.RootContextData[CategoryUpdateDtoValidator.IdKey] = id;
        var result = await _updateValidator.ValidateAsync(context, cancellationToken);
        if (!result.IsValid)
            return ApiResponse<CategoryListDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());

        category.Name = dto.Name.Trim();
        category.IsActive = dto.IsActive;
        await _repository.UpdateAsync(category, cancellationToken);
        return ApiResponse<CategoryListDto>.Ok(ToDto(category), "Kategori güncellendi.");
    }

    public async Task<ApiResponse<CategoryListDto>?> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return null;

        if (category.IsActive)
        {
            category.IsActive = false;
            await _repository.UpdateAsync(category, cancellationToken);
        }
        return ApiResponse<CategoryListDto>.Ok(ToDto(category), "Kategori pasife alındı.");
    }

    private static CategoryListDto ToDto(Category c) => new(c.Id, c.Name, c.IsActive);
}
