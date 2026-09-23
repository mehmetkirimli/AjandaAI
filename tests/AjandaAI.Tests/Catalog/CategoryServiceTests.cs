// CategoryService okuma senaryolarının birim testleridir.
// Repository, bellek içi bir fake ile değiştirilir.

using AjandaAI.Application.Catalog;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Tests.Catalog;

public class CategoryServiceTests
{
    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _items;

        public FakeCategoryRepository(params Category[] items) => _items = items.ToList();

        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_items.Where(c => c.IsActive).ToList());

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));
    }

    private static CategoryService CreateService() => new(new FakeCategoryRepository(
        new Category { Id = 1, Name = "Spor", IsActive = true },
        new Category { Id = 2, Name = "Eski", IsActive = false },
        new Category { Id = 3, Name = "İş", IsActive = true }));

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveCategories()
    {
        var result = await CreateService().GetAllAsync();

        Assert.Equal(new[] { 1, 3 }, result.Select(c => c.Id));
        Assert.All(result, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Spor", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Inactive_StillReturned()
    {
        var result = await CreateService().GetByIdAsync(2);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Null(await CreateService().GetByIdAsync(99));
    }
}
