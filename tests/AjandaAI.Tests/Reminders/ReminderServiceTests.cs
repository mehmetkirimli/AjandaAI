// ReminderService okuma ve yazma senaryolarının birim testleridir (sahte ICurrentUser ile, ADR 0019).
// Repository'ler bellek içi fake'lerle, saat sabit bir TimeProvider ile değiştirilir; validator'lar gerçektir.
// Sahiplik senaryoları: AUTH-45 ve AUTH-41..46'nın Reminder karşılığı (docs/auth-test-senaryolari.md).

using AjandaAI.Application.Common;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Reminders;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Reminders.Validators;
using AjandaAI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Reminders;

public class ReminderServiceTests
{
    private const int Me = 1;
    private const int Other = 2;

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(int userId) => UserId = userId;

        public int UserId { get; }

        public bool IsAdmin => false;
    }

    private sealed class CapturingLogger : ILogger<ReminderService>
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

    private sealed class FakeReminderRepository : IReminderRepository
    {
        private readonly List<Reminder> _items;
        private readonly List<Activity> _activities;

        public FakeReminderRepository(List<Activity> activities, params Reminder[] items)
        {
            _activities = activities;
            _items = items.ToList();
        }

        public IReadOnlyList<Reminder> Items => _items;

        private Activity ActivityOf(Reminder r) => _activities.First(a => a.Id == r.ActivityId);

        public Task<PagedResult<Reminder>> GetPagedAsync(ReminderFilterDto filter, int userId, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(r => ActivityOf(r).UserId == userId).ToList();
            return Task.FromResult(new PagedResult<Reminder>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Reminder?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var r = _items.FirstOrDefault(x => x.Id == id && ActivityOf(x).UserId == userId);
            if (r is not null)
            {
                r.Activity = ActivityOf(r);
            }

            return Task.FromResult(r);
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(x => x.Id == id));

        public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
        {
            reminder.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
            _items.Add(reminder);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly List<Activity> _items;

        public FakeActivityRepository(List<Activity> items) => _items = items;

        public Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, int userId, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(a => a.UserId == userId && a.IsActive).ToList();
            return Task.FromResult(new PagedResult<Activity>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Activity?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.Id == id && a.UserId == userId));

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(a => a.Id == id));

        public Task AddAsync(Activity activity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    private const string ActivityMissingMessage = "Aktivite bulunamadı veya silinmiş.";

    private readonly FakeReminderRepository _reminders;
    private readonly ReminderService _service;

    public ReminderServiceTests()
    {
        (_reminders, _service) = Build(Me, NullLogger<ReminderService>.Instance);
    }

    // 10: benim aktif, 11: benim pasif, 12: başkasının aktif. Hatırlatma 1,2 -> 10; 3 -> 12 (başkasının).
    private static (FakeReminderRepository Reminders, ReminderService Service) Build(int currentUserId, ILogger<ReminderService> logger)
    {
        var activities = new List<Activity>
        {
            new() { Id = 10, UserId = Me, Title = "Koşu", Start = Now.AddDays(1) },
            new() { Id = 11, UserId = Me, Title = "Silinmiş", Start = Now.AddDays(1), IsActive = false },
            new() { Id = 12, UserId = Other, Title = "Başkasının", Start = Now.AddDays(1) }
        };
        var reminders = new FakeReminderRepository(activities,
            new Reminder { Id = 1, ActivityId = 10, RemindAt = Now, Note = "Ayakkabı", CreatedAt = Now },
            new Reminder { Id = 2, ActivityId = 10, RemindAt = Now.AddHours(1), IsSent = true, SentAt = Now },
            new Reminder { Id = 3, ActivityId = 12, RemindAt = Now, Note = "Gizli", CreatedAt = Now });

        var activityRepo = new FakeActivityRepository(activities);
        var currentUser = new FakeCurrentUser(currentUserId);
        var time = new FixedTimeProvider(Now);
        var service = new ReminderService(
            reminders,
            new ReminderCreateDtoValidator(activityRepo, currentUser, time),
            new ReminderUpdateDtoValidator(activityRepo, currentUser, time),
            currentUser,
            time,
            logger);
        return (reminders, service);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOwnMapped()
    {
        var result = (await _service.GetAllAsync(new ReminderFilterDto())).Data!.Items;

        Assert.Equal(new[] { 1, 2 }, result.Select(r => r.Id));
        Assert.True(result[1].IsSent);
    }

    // AUTH-46 (Reminder): liste yalnızca kendi aktivitelerinin hatırlatmalarını döner
    [Fact]
    public async Task AUTH_46_GetAllAsync_ReturnsOnlyOwnReminders()
    {
        var paged = (await _service.GetAllAsync(new ReminderFilterDto())).Data!;

        Assert.DoesNotContain(paged.Items, r => r.Id == 3);
        Assert.Equal(2, paged.TotalCount);

        var (_, otherService) = Build(Other, NullLogger<ReminderService>.Instance);
        var otherPaged = (await otherService.GetAllAsync(new ReminderFilterDto())).Data!;
        Assert.Equal(new[] { 3 }, otherPaged.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Existing_IncludesActivityTitle()
    {
        var result = (await _service.GetByIdAsync(1)).Data;

        Assert.NotNull(result);
        Assert.Equal("Koşu", result!.ActivityTitle);
        Assert.Equal("Ayakkabı", result.Note);
        Assert.Equal(10, result.ActivityId);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNotFound()
    {
        Assert.Equal(ResultType.NotFound, (await _service.GetByIdAsync(99)).ResultType);
    }

    // AUTH-41 (Reminder): başkasının hatırlatması, olmayanla aynı 404 (aynı mesaj) döner
    [Fact]
    public async Task AUTH_41_GetByIdAsync_OtherUsersReminder_ReturnsSameNotFoundAsMissing()
    {
        var foreign = await _service.GetByIdAsync(3);
        var missing = await _service.GetByIdAsync(999);

        Assert.Equal(ResultType.NotFound, foreign.ResultType);
        Assert.Equal(ResultType.NotFound, missing.ResultType);
        Assert.Null(foreign.Data);
        Assert.Equal(missing.Errors, foreign.Errors);
        // Mesaj yalnızca istenen Id'yi içerir; sahiplik bilgisi sızmaz.
        Assert.Equal("Reminder 3 bulunamadı.", foreign.Message);
        Assert.Equal("Reminder 999 bulunamadı.", missing.Message);
    }

    // AUTH-42 (Reminder): başkasının hatırlatması PUT / DELETE edilemez, kayıt değişmez
    [Fact]
    public async Task AUTH_42_UpdateAndDelete_OtherUsersReminder_ReturnNotFound_AndRecordUnchanged()
    {
        var update = await _service.UpdateAsync(3, new ReminderUpdateDto(10, Now.AddHours(3), "Ele geçirdim"));
        var delete = await _service.DeleteAsync(3);

        Assert.Equal(ResultType.NotFound, update.ResultType);
        Assert.Equal(ResultType.NotFound, delete.ResultType);
        var stored = _reminders.Items.Single(r => r.Id == 3);
        Assert.Equal(12, stored.ActivityId);
        Assert.Equal("Gizli", stored.Note);
        Assert.Equal(3, _reminders.Items.Count);
    }

    // AUTH-43 (Reminder): başkasının kaydına erişim Warning loglar (UserId, ReminderId)
    [Fact]
    public async Task AUTH_43_OtherUsersReminder_LogsWarning_ForGetPutDelete()
    {
        var logger = new CapturingLogger();
        var (_, service) = Build(Me, logger);

        await service.GetByIdAsync(3);
        await service.UpdateAsync(3, new ReminderUpdateDto(10, Now.AddHours(3), null));
        await service.DeleteAsync(3);

        var warnings = logger.Entries.Where(e => e.Template == "Yetkisiz erişim denemesi {UserId} {ReminderId}").ToList();
        Assert.Equal(3, warnings.Count);
        Assert.All(warnings, w =>
        {
            Assert.Equal(LogLevel.Warning, w.Level);
            Assert.Equal(Me, w.Values["UserId"]);
            Assert.Equal(3, w.Values["ReminderId"]);
        });
    }

    // AUTH-44 (Reminder): olmayan Id log yazmaz; kendi kaydına erişim de yazmaz
    [Fact]
    public async Task AUTH_44_MissingId_And_OwnReminder_WriteNoWarning()
    {
        var logger = new CapturingLogger();
        var (_, service) = Build(Me, logger);

        await service.GetByIdAsync(99);
        await service.UpdateAsync(99, new ReminderUpdateDto(10, Now.AddHours(1), null));
        await service.DeleteAsync(99);
        await service.GetByIdAsync(1);

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndReturnsDetail()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(10, Now.AddHours(2), "Su al"));

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.Id);
        Assert.Equal("Koşu", result.Data.ActivityTitle);
        Assert.Equal(Now, result.Data.CreatedAt);
        Assert.Equal(4, _reminders.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_MissingActivity_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(99, Now.AddHours(2), null));

        Assert.False(result.Success);
        Assert.Contains(ActivityMissingMessage, result.Errors);
        Assert.Equal(3, _reminders.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_InactiveActivity_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(11, Now.AddHours(2), null));

        Assert.False(result.Success);
        Assert.Contains(ActivityMissingMessage, result.Errors);
        Assert.Equal(3, _reminders.Items.Count);
    }

    // AUTH-45: başkasının aktivitesine hatırlatma eklenemez; hata, olmayan aktivite ile birebir aynıdır
    [Fact]
    public async Task AUTH_45_CreateAsync_OtherUsersActivity_Fails_WithSameErrorAsMissingActivity()
    {
        var foreign = await _service.CreateAsync(new ReminderCreateDto(12, Now.AddHours(2), null));
        var missing = await _service.CreateAsync(new ReminderCreateDto(99, Now.AddHours(2), null));

        Assert.False(foreign.Success);
        Assert.Equal(ResultType.ValidationError, foreign.ResultType);
        Assert.Equal(missing.Errors, foreign.Errors);
        Assert.Equal(missing.Message, foreign.Message);
        Assert.Equal(new[] { ActivityMissingMessage }, foreign.Errors);
        Assert.Equal(3, _reminders.Items.Count);
    }

    // AUTH-45: güncellemede ActivityId'yi başkasının aktivitesine çevirmek de reddedilir
    [Fact]
    public async Task AUTH_45_UpdateAsync_MoveToOtherUsersActivity_Fails_AndRecordUnchanged()
    {
        var foreign = await _service.UpdateAsync(1, new ReminderUpdateDto(12, Now.AddHours(3), null));
        var missing = await _service.UpdateAsync(1, new ReminderUpdateDto(99, Now.AddHours(3), null));

        Assert.False(foreign.Success);
        Assert.Equal(missing.Errors, foreign.Errors);
        Assert.Equal(new[] { ActivityMissingMessage }, foreign.Errors);
        Assert.Equal(10, _reminders.Items.Single(r => r.Id == 1).ActivityId);
    }

    [Fact]
    public async Task CreateAsync_PastRemindAt_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(10, Now.AddMinutes(-1), null));

        Assert.False(result.Success);
        Assert.Contains("RemindAt geçmiş bir tarih olamaz.", result.Errors);
    }

    [Fact]
    public async Task CreateAsync_RemindAtAfterActivityStart_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(10, Now.AddDays(2), null));

        Assert.False(result.Success);
        Assert.Contains("RemindAt, Activity başlangıç zamanından sonra olamaz.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_Valid_UpdatesFields()
    {
        var result = await _service.UpdateAsync(1, new ReminderUpdateDto(10, Now.AddHours(3), "Yeni not"));

        Assert.True(result.Success);
        Assert.Equal("Yeni not", result.Data!.Note);
        Assert.Equal(Now.AddHours(3), result.Data.RemindAt);
    }

    [Fact]
    public async Task UpdateAsync_RemindAtAfterActivityStart_Fails()
    {
        var result = await _service.UpdateAsync(1, new ReminderUpdateDto(10, Now.AddDays(2), null));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNotFound()
    {
        Assert.Equal(ResultType.NotFound, (await _service.UpdateAsync(99, new ReminderUpdateDto(10, Now.AddHours(1), null))).ResultType);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAndMissing()
    {
        Assert.True((await _service.DeleteAsync(1)).Success);
        Assert.Equal(ResultType.NotFound, (await _service.DeleteAsync(1)).ResultType);
        Assert.Equal(2, _reminders.Items.Count);
    }
}
