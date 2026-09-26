// ReminderService okuma ve yazma senaryolarının birim testleridir.
// Repository'ler bellek içi fake'lerle, saat sabit bir TimeProvider ile değiştirilir.
// Validator'lar gerçek sınıflardır (ilişkisel kurallar da test edilir).

using AjandaAI.Application.Common;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Reminders;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Reminders.Validators;
using AjandaAI.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Reminders;

public class ReminderServiceTests
{
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

        public Task<PagedResult<Reminder>> GetPagedAsync(ReminderFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = _items.ToList();
            return Task.FromResult(new PagedResult<Reminder>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Reminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var r = _items.FirstOrDefault(x => x.Id == id);
            if (r is not null)
            {
                r.Activity = _activities.First(a => a.Id == r.ActivityId);
            }

            return Task.FromResult(r);
        }

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

        public Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(a => a.IsActive).ToList();
            return Task.FromResult(new PagedResult<Activity>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(a => a.Id == id && a.IsActive));

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

    private readonly FakeReminderRepository _reminders;
    private readonly ReminderService _service;

    public ReminderServiceTests()
    {
        var activities = new List<Activity>
        {
            new() { Id = 10, Title = "Koşu", Start = Now.AddDays(1) },
            new() { Id = 11, Title = "Silinmiş", Start = Now.AddDays(1), IsActive = false }
        };
        _reminders = new FakeReminderRepository(activities,
            new Reminder { Id = 1, ActivityId = 10, RemindAt = Now, Note = "Ayakkabı", CreatedAt = Now },
            new Reminder { Id = 2, ActivityId = 10, RemindAt = Now.AddHours(1), IsSent = true, SentAt = Now });

        var activityRepo = new FakeActivityRepository(activities);
        var time = new FixedTimeProvider(Now);
        _service = new ReminderService(
            _reminders,
            new ReminderCreateDtoValidator(activityRepo, time),
            new ReminderUpdateDtoValidator(activityRepo, time),
            time,
            NullLogger<ReminderService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMapped()
    {
        var result = (await _service.GetAllAsync(new ReminderFilterDto())).Data!.Items;

        Assert.Equal(new[] { 1, 2 }, result.Select(r => r.Id));
        Assert.True(result[1].IsSent);
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
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await _service.GetByIdAsync(99)).ResultType);
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndReturnsDetail()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(10, Now.AddHours(2), "Su al"));

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Id);
        Assert.Equal("Koşu", result.Data.ActivityTitle);
        Assert.Equal(Now, result.Data.CreatedAt);
        Assert.Equal(3, _reminders.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_MissingActivity_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(99, Now.AddHours(2), null));

        Assert.False(result.Success);
        Assert.Contains("Aktivite bulunamadı veya silinmiş.", result.Errors);
        Assert.Equal(2, _reminders.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_InactiveActivity_Fails()
    {
        var result = await _service.CreateAsync(new ReminderCreateDto(11, Now.AddHours(2), null));

        Assert.False(result.Success);
        Assert.Contains("Aktivite bulunamadı veya silinmiş.", result.Errors);
        Assert.Equal(2, _reminders.Items.Count);
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

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Yeni not", result.Data!.Note);
        Assert.Equal(Now.AddHours(3), result.Data.RemindAt);
    }

    [Fact]
    public async Task UpdateAsync_RemindAtAfterActivityStart_Fails()
    {
        var result = await _service.UpdateAsync(1, new ReminderUpdateDto(10, Now.AddDays(2), null));

        Assert.NotNull(result);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await _service.UpdateAsync(99, new ReminderUpdateDto(10, Now.AddHours(1), null))).ResultType);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAndMissing()
    {
        Assert.True((await _service.DeleteAsync(1)).Success);
        Assert.Equal(ResultType.NotFound, (await _service.DeleteAsync(1)).ResultType);
        Assert.Single(_reminders.Items);
    }
}
