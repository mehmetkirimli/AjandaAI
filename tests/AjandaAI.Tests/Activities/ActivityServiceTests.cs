// ActivityService okuma ve yazma senaryolarının birim testleridir (sahte ICurrentUser ile, ADR 0019).
// Repository'ler bellek içi fake'lerle değiştirilir; validator'lar gerçektir.
// Sahiplik senaryoları: AUTH-41, 42, 43, 44, 46, 47 (docs/auth-test-senaryolari.md).

using AjandaAI.Application.Common;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Activities.Validators;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Activities;

public class ActivityServiceTests
{
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(int userId) => UserId = userId;

        public int UserId { get; }

        public bool IsAdmin => false;
    }

    private sealed class CapturingLogger : ILogger<ActivityService>
    {
        public List<(LogLevel Level, string Template, Dictionary<string, object?> Values)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = (state as IEnumerable<KeyValuePair<string, object?>>)?.ToDictionary(p => p.Key, p => p.Value)
                ?? new Dictionary<string, object?>();
            Entries.Add((logLevel, values.GetValueOrDefault("{OriginalFormat}") as string ?? string.Empty, values));
        }
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly List<Activity> _items;

        public FakeActivityRepository(params Activity[] items) => _items = items.ToList();

        public int ExistsCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, int userId, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(a => a.UserId == userId && a.IsActive).ToList();
            return Task.FromResult(new PagedResult<Activity>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Activity?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.Id == id && a.UserId == userId));

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            ExistsCalls++;
            return Task.FromResult(_items.Any(a => a.Id == id));
        }

        public Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
        {
            activity.Id = _items.Count == 0 ? 1 : _items.Max(a => a.Id) + 1;
            _items.Add(activity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Activity Single(int id) => _items.Single(a => a.Id == id);
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

    private const int Me = 7;
    private const int Other = 9;

    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private static ActivityService CreateService(
        FakeActivityRepository? repository = null, int currentUserId = Me, ILogger<ActivityService>? logger = null)
    {
        var categories = new FakeCategoryRepository();
        return new ActivityService(
            repository ?? CreateRepository(),
            new ActivityCreateDtoValidator(categories),
            new ActivityUpdateDtoValidator(categories),
            new ActivityFilterDtoValidator(),
            new FakeCurrentUser(currentUserId),
            logger ?? NullLogger<ActivityService>.Instance);
    }

    // 1 ve 2: Me'nin (7); 3: başka kullanıcının (9).
    private static FakeActivityRepository CreateRepository() => new(
        new Activity
        {
            Id = 1, UserId = Me, CategoryId = 6, Title = "Koşu", Description = "Sahil",
            Status = ActivityStatus.Completed, Priority = Priority.High, EnergyLevel = EnergyLevel.High,
            Start = BaseTime, End = BaseTime.AddHours(1), Location = "Moda",
            EstimatedBudget = 0m, Rating = 8, WouldRepeat = true
        },
        new Activity
        {
            Id = 2, UserId = Me, CategoryId = 1, Title = "Akşam yemeği",
            Status = ActivityStatus.Planned, Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low,
            Start = BaseTime.AddDays(1), End = BaseTime.AddDays(1).AddHours(2), EstimatedBudget = 1500m
        },
        new Activity
        {
            Id = 3, UserId = Other, CategoryId = 1, Title = "Gizli plan",
            Status = ActivityStatus.Planned, Priority = Priority.Low, EnergyLevel = EnergyLevel.Low,
            Start = BaseTime.AddDays(2), End = BaseTime.AddDays(2).AddHours(1), EstimatedBudget = 10m
        });

    private static ActivityCreateDto ValidCreate(
        int categoryId = 6, string title = "Yüzme",
        ActivityStatus status = ActivityStatus.Planned, int? rating = null,
        decimal budget = 0m, int durationHours = 1) =>
        new(categoryId, title, "", status, Priority.Low, EnergyLevel.Medium,
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

    // AUTH-46
    [Fact]
    public async Task AUTH_46_GetAllAsync_ReturnsOnlyCurrentUsersActivities()
    {
        var mine = (await CreateService(currentUserId: Me).GetAllAsync(new ActivityFilterDto())).Data!.Items;
        var theirs = (await CreateService(currentUserId: Other).GetAllAsync(new ActivityFilterDto())).Data!.Items;
        var stranger = (await CreateService(currentUserId: 55).GetAllAsync(new ActivityFilterDto())).Data!;

        Assert.Equal(new[] { 1, 2 }, mine.Select(a => a.Id));
        Assert.Equal(new[] { 3 }, theirs.Select(a => a.Id));
        Assert.Empty(stranger.Items);
        Assert.Equal(0, stranger.TotalCount);
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
    public async Task GetByIdAsync_Missing_ReturnsNotFound()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().GetByIdAsync(99)).ResultType);
    }

    // AUTH-41: başkasının kaydı ile hiç olmayan kayıt, aynı Id için birebir aynı sonucu verir.
    [Fact]
    public async Task AUTH_41_GetByIdAsync_OtherUsersActivity_IsIdenticalToMissingActivity()
    {
        var foreign = await CreateService(CreateRepository()).GetByIdAsync(3);
        var missing = await CreateService(new FakeActivityRepository()).GetByIdAsync(3);

        Assert.Equal(ResultType.NotFound, foreign.ResultType);
        Assert.Equal(missing.ResultType, foreign.ResultType);
        Assert.Equal(missing.Success, foreign.Success);
        Assert.Equal(missing.Message, foreign.Message);
        Assert.Equal(missing.Errors, foreign.Errors);
        Assert.Null(foreign.Data);
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsOkWithNewId()
    {
        var service = CreateService();

        var result = await service.CreateAsync(ValidCreate());

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.Id);
        Assert.Equal("Planned", result.Data.Status);
        Assert.NotNull((await service.GetByIdAsync(4)).Data);
    }

    // AUTH-47 (servis düzeyi: DTO'da UserId yok, sahip ICurrentUser'dır)
    [Fact]
    public async Task AUTH_47_CreateAsync_OwnerIsCurrentUser()
    {
        var repository = CreateRepository();

        var result = await CreateService(repository, currentUserId: 21).CreateAsync(ValidCreate());

        Assert.True(result.Success);
        Assert.Equal(21, result.Data!.UserId);
        Assert.Equal(21, repository.Single(result.Data.Id).UserId);
    }

    [Theory]
    [InlineData(9, "Kategori bulunamadı veya aktif değil.")]
    [InlineData(42, "Kategori bulunamadı veya aktif değil.")]
    public async Task CreateAsync_InvalidCategory_Fails(int categoryId, string expected)
    {
        var result = await CreateService().CreateAsync(ValidCreate(categoryId: categoryId));

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
        Assert.Equal(Me, result.Data.UserId);
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
    public async Task UpdateAsync_Missing_ReturnsNotFound()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().UpdateAsync(99, ValidUpdate())).ResultType);
    }

    // AUTH-42
    [Fact]
    public async Task AUTH_42_UpdateAsync_OtherUsersActivity_ReturnsNotFound_AndDoesNotChangeIt()
    {
        var repository = CreateRepository();

        var result = await CreateService(repository).UpdateAsync(3, ValidUpdate());

        Assert.Equal(ResultType.NotFound, result.ResultType);
        Assert.Equal(0, repository.UpdateCalls);
        Assert.Equal("Gizli plan", repository.Single(3).Title);
    }

    // AUTH-42
    [Fact]
    public async Task AUTH_42_DeleteAsync_OtherUsersActivity_ReturnsNotFound_AndKeepsItActive()
    {
        var repository = CreateRepository();

        var result = await CreateService(repository).DeleteAsync(3);

        Assert.Equal(ResultType.NotFound, result.ResultType);
        Assert.Equal(0, repository.UpdateCalls);
        Assert.True(repository.Single(3).IsActive);
    }

    // AUTH-43
    [Fact]
    public async Task AUTH_43_OtherUsersActivity_WritesUnauthorizedAccessWarning_WithUserIdAndActivityId()
    {
        var logger = new CapturingLogger();

        await CreateService(logger: logger).GetByIdAsync(3);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Yetkisiz erişim denemesi {UserId} {ActivityId}", entry.Template);
        Assert.Equal(Me, entry.Values["UserId"]);
        Assert.Equal(3, entry.Values["ActivityId"]);
    }

    // AUTH-43
    [Fact]
    public async Task AUTH_43_UpdateAndDelete_OnOtherUsersActivity_AlsoWriteWarning()
    {
        var logger = new CapturingLogger();
        var service = CreateService(logger: logger);

        await service.UpdateAsync(3, ValidUpdate());
        await service.DeleteAsync(3);

        Assert.Equal(2, logger.Entries.Count(e => e.Level == LogLevel.Warning && e.Template.StartsWith("Yetkisiz erişim denemesi")));
    }

    // AUTH-44
    [Fact]
    public async Task AUTH_44_MissingActivity_ReturnsNotFound_WithoutWarningLog()
    {
        var logger = new CapturingLogger();
        var service = CreateService(logger: logger);

        var result = await service.GetByIdAsync(99);
        await service.UpdateAsync(99, ValidUpdate());
        await service.DeleteAsync(99);

        Assert.Equal(ResultType.NotFound, result.ResultType);
        Assert.Empty(logger.Entries);
    }

    // AUTH-44 + ADR 0018: ExistsAsync yalnızca hata yolunda çalışır
    [Fact]
    public async Task AUTH_44_ExistsAsync_RunsOnlyOnErrorPath_NotOnSuccess()
    {
        var repository = CreateRepository();
        var service = CreateService(repository);

        await service.GetByIdAsync(1);
        Assert.Equal(0, repository.ExistsCalls);

        await service.GetByIdAsync(99);
        Assert.Equal(1, repository.ExistsCalls);
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
    public async Task DeleteAsync_Missing_ReturnsNotFound()
    {
        Assert.Equal(ResultType.NotFound, (await CreateService().DeleteAsync(99)).ResultType);
    }
}
