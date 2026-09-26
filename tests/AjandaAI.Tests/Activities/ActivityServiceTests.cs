// ActivityService okuma ve yazma senaryolarının birim testleridir.
// Repository'ler bellek içi fake'lerle değiştirilir; validator'lar gerçektir.

using AjandaAI.Application.Common;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Activities.Validators;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Users;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Activities;

public class ActivityServiceTests
{
    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly List<Activity> _items;

        public FakeActivityRepository(params Activity[] items) => _items = items.ToList();

        public Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(a => a.IsActive).ToList();
            return Task.FromResult(new PagedResult<Activity>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(a => a.Id == id && a.IsActive));

        public Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
        {
            activity.Id = _items.Count == 0 ? 1 : _items.Max(a => a.Id) + 1;
            _items.Add(activity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _items = new()
        {
            new Category { Id = 1, Name = "Sosyal", IsActive = true },
            new Category { Id = 6, Name = "Spor", IsActive = true },
            new Category { Id = 9, Name = "Eski", IsActive = false }
        };

        public Task<PagedResult<Category>> GetPagedAsync(CategoryFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(c => c.IsActive).ToList();
            return Task.FromResult(new PagedResult<Category>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(c => c.Id == id && c.IsActive));

        public Task AddAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _items = new() { new User { Id = 7 }, new User { Id = 8, IsActive = false } };

        public Task<PagedResult<User>> GetPagedAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(u => u.IsActive).ToList();
            return Task.FromResult(new PagedResult<User>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(u => u.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(u => u.Id == id && u.IsActive));

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private static ActivityService CreateService()
    {
        var categories = new FakeCategoryRepository();
        return new ActivityService(
            CreateRepository(),
            new ActivityCreateDtoValidator(categories, new FakeUserRepository()),
            new ActivityUpdateDtoValidator(categories),
            new ActivityFilterDtoValidator(),
            NullLogger<ActivityService>.Instance);
    }

    private static FakeActivityRepository CreateRepository() => new(
        new Activity
        {
            Id = 1, UserId = 7, CategoryId = 6, Title = "Koşu", Description = "Sahil",
            Status = ActivityStatus.Completed, Priority = Priority.High, EnergyLevel = EnergyLevel.High,
            Start = BaseTime, End = BaseTime.AddHours(1), Location = "Moda",
            EstimatedBudget = 0m, Rating = 8, WouldRepeat = true
        },
        new Activity
        {
            Id = 2, UserId = 7, CategoryId = 1, Title = "Akşam yemeği",
            Status = ActivityStatus.Planned, Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low,
            Start = BaseTime.AddDays(1), End = BaseTime.AddDays(1).AddHours(2), EstimatedBudget = 1500m
        });

    private static ActivityCreateDto ValidCreate(
        int userId = 7, int categoryId = 6, string title = "Yüzme",
        ActivityStatus status = ActivityStatus.Planned, int? rating = null,
        decimal budget = 0m, int durationHours = 1) =>
        new(userId, categoryId, title, "", status, Priority.Low, EnergyLevel.Medium,
            BaseTime, BaseTime.AddHours(durationHours), false, null, false, budget, rating, null);

    private static ActivityUpdateDto ValidUpdate(ActivityStatus status = ActivityStatus.Completed, int? rating = 9) =>
        new(6, "Koşu 2", "", status, Priority.Low, EnergyLevel.Medium,
            BaseTime, BaseTime.AddHours(2), false, null, false, 0m, rating, true);

    [Fact]
    public async Task GetAllAsync_ReturnsAllMappedDtos()
    {
        var result = (await CreateService().GetAllAsync(new ActivityFilterDto())).Data!.Items;

        Assert.Equal(new[] { 1, 2 }, result.Select(a => a.Id));
        Assert.Equal("Koşu", result[0].Title);
        Assert.Equal("Completed", result[0].Status);
        Assert.Equal("Medium", result[1].Priority);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDetailDto()
    {
        var result = (await CreateService().GetByIdAsync(1)).Data;

        Assert.NotNull(result);
        Assert.Equal("Koşu", result!.Title);
        Assert.Equal("High", result.EnergyLevel);
        Assert.Equal("Moda", result.Location);
        Assert.Equal(8, result.Rating);
        Assert.True(result.WouldRepeat);
    }

    [Fact]
    public async Task GetByIdAsync_NullableFieldsEmpty_MappedAsNull()
    {
        var result = (await CreateService().GetByIdAsync(2)).Data;

        Assert.NotNull(result);
        Assert.Null(result!.Location);
        Assert.Null(result.Rating);
        Assert.Null(result.WouldRepeat);
        Assert.Equal(1500m, result.EstimatedBudget);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().GetByIdAsync(99)).ResultType);
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsOkWithNewId()
    {
        var service = CreateService();

        var result = await service.CreateAsync(ValidCreate());

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Id);
        Assert.Equal("Planned", result.Data.Status);
        Assert.NotNull((await service.GetByIdAsync(3)).Data);
    }

    [Theory]
    [InlineData(99, 6, "Kullanıcı bulunamadı veya pasif.")]
    [InlineData(8, 6, "Kullanıcı bulunamadı veya pasif.")]
    [InlineData(7, 9, "Kategori bulunamadı veya aktif değil.")]
    [InlineData(7, 42, "Kategori bulunamadı veya aktif değil.")]
    public async Task CreateAsync_InvalidRelations_Fails(int userId, int categoryId, string expected)
    {
        var result = await CreateService().CreateAsync(ValidCreate(userId: userId, categoryId: categoryId));

        Assert.False(result.Success);
        Assert.Contains(expected, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_FieldRules_CollectsAllErrors()
    {
        var dto = ValidCreate(title: new string('x', 201), rating: 11, budget: -1m, durationHours: -1);

        var result = await CreateService().CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Contains("Başlık en fazla 200 karakter olabilir.", result.Errors);
        Assert.Contains("Bitiş zamanı başlangıçtan sonra olmalıdır.", result.Errors);
        Assert.Contains("Puan 1 ile 10 arasında olmalıdır.", result.Errors);
        Assert.Contains("Puan yalnızca tamamlanan aktivitelere verilebilir.", result.Errors);
        Assert.Contains("Tahmini bütçe negatif olamaz.", result.Errors);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Fails()
    {
        var result = await CreateService().CreateAsync(ValidCreate(title: ""));

        Assert.False(result.Success);
        Assert.Contains("Başlık zorunludur.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var result = await CreateService().UpdateAsync(2, ValidUpdate());

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Koşu 2", result.Data!.Title);
        Assert.Equal(9, result.Data.Rating);
        Assert.Equal(7, result.Data.UserId);
    }

    [Fact]
    public async Task UpdateAsync_RatingWithoutCompleted_Fails()
    {
        var result = await CreateService().UpdateAsync(1, ValidUpdate(status: ActivityStatus.InProgress, rating: 5));

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Contains("Puan yalnızca tamamlanan aktivitelere verilebilir.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().UpdateAsync(99, ValidUpdate())).ResultType);
    }

    [Fact]
    public async Task DeleteAsync_Existing_SetsIsActiveFalse_DoesNotRemove()
    {
        var service = CreateService();

        Assert.True((await service.DeleteAsync(1)).Success);

        var detail = (await service.GetByIdAsync(1)).Data;
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);
        Assert.DoesNotContain((await service.GetAllAsync(new ActivityFilterDto())).Data!.Items, a => a.Id == 1);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ReturnsFalse()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().DeleteAsync(99)).ResultType);
    }
}
