// CategoryService okuma ve yazma senaryolarının birim testleridir.
// Repository bellek içi bir fake ile değiştirilir; validator'lar gerçek sınıflardır.

using AjandaAI.Application.Common;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Categories.Validators;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Tests.Categories;

public class CategoryServiceTests
{
    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Items { get; }

        public FakeCategoryRepository(params Category[] items) => Items = items.ToList();

        public Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(Items.Where(c => c.IsActive).ToList());

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c => c.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(c => c.Id == id && c.IsActive));

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            category.Id = Items.Count == 0 ? 1 : Items.Max(c => c.Id) + 1;
            Items.Add(category);
            return Task.CompletedTask;
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(c =>
                string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase) && c.Id != excludeId));

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static (CategoryService Service, FakeCategoryRepository Repo) Create()
    {
        var repo = new FakeCategoryRepository(
            new Category { Id = 1, Name = "Spor", IsActive = true },
            new Category { Id = 2, Name = "Eski", IsActive = false },
            new Category { Id = 3, Name = "İş", IsActive = true });
        var service = new CategoryService(repo,
            new CategoryCreateDtoValidator(repo),
            new CategoryUpdateDtoValidator(repo));
        return (service, repo);
    }

    private static CategoryService CreateService() => Create().Service;

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveCategories()
    {
        var result = (await CreateService().GetAllAsync()).Data!;

        Assert.Equal(new[] { 1, 3 }, result.Select(c => c.Id));
        Assert.All(result, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var result = (await CreateService().GetByIdAsync(1)).Data;

        Assert.NotNull(result);
        Assert.Equal("Spor", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Inactive_StillReturned()
    {
        var result = (await CreateService().GetByIdAsync(2)).Data;

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().GetByIdAsync(99)).ResultType);
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsActiveCategory()
    {
        var (service, repo) = Create();

        var response = await service.CreateAsync(new CategoryCreateDto("Sosyal"));

        Assert.True(response.Success);
        Assert.True(response.Data!.IsActive);
        Assert.Contains(repo.Items, c => c.Name == "Sosyal");
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsFail()
    {
        var response = await CreateService().CreateAsync(new CategoryCreateDto(""));

        Assert.False(response.Success);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFail()
    {
        var response = await CreateService().CreateAsync(new CategoryCreateDto("spor"));

        Assert.False(response.Success);
        Assert.Contains("Bu isimde bir kategori zaten var.", response.Errors);
    }

    [Fact]
    public async Task UpdateAsync_SameNameOnSelf_Succeeds()
    {
        var response = await CreateService().UpdateAsync(1, new CategoryUpdateDto("Spor", true));

        Assert.NotNull(response);
        Assert.True(response!.Success);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().UpdateAsync(99, new CategoryUpdateDto("X", true))).ResultType);
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse_DoesNotRemove()
    {
        var (service, repo) = Create();

        var response = await service.DeactivateAsync(1);

        Assert.True(response!.Success);
        Assert.False(repo.Items.Single(c => c.Id == 1).IsActive);
        Assert.Equal(3, repo.Items.Count);
    }

    [Fact]
    public async Task DeactivateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().DeactivateAsync(99)).ResultType);
    }
}
