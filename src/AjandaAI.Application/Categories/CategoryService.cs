// Category okuma senaryolarını yöneten uygulama servisidir.
// Liste yalnızca aktif kategorileri döner; Id ile sorgu pasif kaydı da döner
// (eski aktiviteler pasife alınmış kategoriye bağlı olabilir).

using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Categories;

public class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
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

    private static CategoryListDto ToDto(Category c) => new(c.Id, c.Name, c.IsActive);
}
