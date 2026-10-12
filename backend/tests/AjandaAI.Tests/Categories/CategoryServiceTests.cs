// CategoryService okuma ve yazma senaryolarının birim testleridir.
// Repository bellek içi bir fake ile değiştirilir; validator'lar gerçek sınıflardır.

using AjandaAI.Application.Common;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Categories.Validators;
using AjandaAI.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Categories;

public class CategoryServiceTests
{
    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Items { get; }

        public FakeCategoryRepository(params Category[] items) => Items = items.ToList();

        public Task<PagedResult<Category>> GetPagedAsync(CategoryFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = Items.Where(c => filter.IncludeInactive || c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList();
            return Task.FromResult(new PagedResult<Category>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

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

        public int ReorderCalls { get; private set; }

        public Task<List<int>> GetAllIdsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Select(c => c.Id).ToList());

        public Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count == 0 ? 0 : Items.Max(c => c.SortOrder));

        public Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
        {
            ReorderCalls++;
            for (var i = 0; i < orderedIds.Count; i++)
                Items.Single(c => c.Id == orderedIds[i]).SortOrder = i + 1;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public int UserId => 1;
        public bool IsAdmin { get; init; }
    }

    private static (CategoryService Service, FakeCategoryRepository Repo) Create(bool isAdmin = false)
    {
        var repo = new FakeCategoryRepository(
            new Category { Id = 1, Name = "Spor", SortOrder = 1, IsActive = true },
            new Category { Id = 2, Name = "Eski", SortOrder = 2, IsActive = false },
            new Category { Id = 3, Name = "İş", SortOrder = 3, IsActive = true });
        var service = new CategoryService(repo,
            new CategoryCreateDtoValidator(repo),
            new CategoryUpdateDtoValidator(repo),
            new FakeCurrentUser { IsAdmin = isAdmin },
            NullLogger<CategoryService>.Instance);
        return (service, repo);
    }

    private static CategoryService CreateService() => Create().Service;

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveCategories()
    {
        var result = (await CreateService().GetAllAsync(new CategoryFilterDto())).Data!.Items;

        Assert.Equal(new[] { 1, 3 }, result.Select(c => c.Id));
        Assert.All(result, c => Assert.True(c.IsActive));
    }

    // ADMIN-11 (servis düzeyi): Admin includeInactive=true ile aktif + pasif, SortOrder sırasıyla.
    [Fact]
    public async Task ADMIN_11_Admin_IncludeInactive_ReturnsAllInSortOrder()
    {
        var result = (await Create(isAdmin: true).Service.GetAllAsync(new CategoryFilterDto { IncludeInactive = true })).Data!.Items;

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(c => c.Id));
    }

    // ADMIN-11 (servis düzeyi): User rolünde parametre sessizce yok sayılır.
    [Fact]
    public async Task ADMIN_11_User_IncludeInactive_IsIgnored()
    {
        var result = (await Create(isAdmin: false).Service.GetAllAsync(new CategoryFilterDto { IncludeInactive = true })).Data!.Items;

        Assert.Equal(new[] { 1, 3 }, result.Select(c => c.Id));
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

    // ADMIN-03 (unit): geçerli tam liste -> NoContent, SortOrder 1..N
    [Fact]
    public async Task ADMIN_03_Reorder_ValidFullList_ReturnsNoContent_AndAssigns1ToN()
    {
        var (service, repo) = Create();

        var response = await service.ReorderAsync(new CategoryOrderDto(new[] { 3, 1, 2 }));

        Assert.Equal(ResultType.NoContent, response.ResultType);
        Assert.Equal(1, repo.Items.Single(c => c.Id == 3).SortOrder);
        Assert.Equal(2, repo.Items.Single(c => c.Id == 1).SortOrder);
        Assert.Equal(3, repo.Items.Single(c => c.Id == 2).SortOrder);
    }

    // ADMIN-04, ADMIN-05, ADMIN-06 (unit): eksik / bilinmeyen / tekrarlı -> ValidationError, hiçbir şey değişmez
    [Theory]
    [InlineData(new[] { 3, 1 })]          // ADMIN-04 eksik
    [InlineData(new[] { 3, 1, 2, 99 })]   // ADMIN-05 bilinmeyen
    [InlineData(new[] { 3, 1, 2, 1 })]    // ADMIN-06 tekrarlı
    [InlineData(new[] { 3, 3, 2 })]       // ADMIN-06 tekrarlı + eksik
    public async Task ADMIN_04_05_06_Reorder_InvalidList_ReturnsValidationError_AndNothingChanges(int[] ids)
    {
        var (service, repo) = Create();

        var response = await service.ReorderAsync(new CategoryOrderDto(ids));

        Assert.Equal(ResultType.ValidationError, response.ResultType);
        Assert.NotEmpty(response.Errors);
        Assert.Equal(0, repo.ReorderCalls);
        Assert.Equal(new[] { 1, 2, 3 }, repo.Items.OrderBy(c => c.Id).Select(c => c.SortOrder));
    }

    // ADMIN-04 (unit): boş/null liste eksik sayılır
    [Fact]
    public async Task ADMIN_04_Reorder_NullList_ReturnsValidationError()
    {
        var (service, repo) = Create();

        var response = await service.ReorderAsync(new CategoryOrderDto(null));

        Assert.Equal(ResultType.ValidationError, response.ResultType);
        Assert.Equal(0, repo.ReorderCalls);
    }

    // ADMIN-10 (unit): pasif kategori (Id 2) listede olmalı; dahil edilince geçerli
    [Fact]
    public async Task ADMIN_10_Reorder_ListWithoutInactive_IsRejected_WithInactive_IsAccepted()
    {
        var (service, _) = Create();

        var without = await service.ReorderAsync(new CategoryOrderDto(new[] { 3, 1 }));
        var with = await service.ReorderAsync(new CategoryOrderDto(new[] { 2, 3, 1 }));

        Assert.Equal(ResultType.ValidationError, without.ResultType);
        Assert.Equal(ResultType.NoContent, with.ResultType);
    }

    // ADMIN-08 (unit): yeni kategori max + 1 (pasifler dahil)
    [Fact]
    public async Task ADMIN_08_Create_AssignsMaxPlusOne()
    {
        var (service, _) = Create();

        var response = await service.CreateAsync(new CategoryCreateDto("Yeni"));

        Assert.Equal(4, response.Data!.SortOrder);
    }

    // ADMIN-09 (unit): pasife alma ve geri açma SortOrder'ı değiştirmez
    [Fact]
    public async Task ADMIN_09_DeactivateThenReactivate_KeepsSortOrder()
    {
        var (service, repo) = Create();

        await service.DeactivateAsync(1);
        var reactivated = await service.UpdateAsync(1, new CategoryUpdateDto("Spor", true));

        Assert.Equal(1, reactivated.Data!.SortOrder);
        Assert.Equal(1, repo.Items.Single(c => c.Id == 1).SortOrder);
    }

    [Fact]
    public async Task DeactivateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().DeactivateAsync(99)).ResultType);
    }
}
