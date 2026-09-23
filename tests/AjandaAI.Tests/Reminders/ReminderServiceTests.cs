// ReminderService okuma senaryolarının birim testleridir.
// Repository, bellek içi bir fake ile değiştirilir.

using AjandaAI.Application.Reminders;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Tests.Reminders;

public class ReminderServiceTests
{
    private sealed class FakeReminderRepository : IReminderRepository
    {
        private readonly List<Reminder> _items;

        public FakeReminderRepository(params Reminder[] items) => _items = items.ToList();

        public Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Reminder>>(_items.ToList());

        public Task<Reminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.Id == id));
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    private static ReminderService CreateService()
    {
        var activity = new Activity { Id = 10, Title = "Koşu" };
        return new(new FakeReminderRepository(
            new Reminder { Id = 1, ActivityId = 10, Activity = activity, RemindAt = Now, Note = "Ayakkabı", CreatedAt = Now },
            new Reminder { Id = 2, ActivityId = 10, Activity = activity, RemindAt = Now.AddHours(1), IsSent = true, SentAt = Now }));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMapped()
    {
        var result = await CreateService().GetAllAsync();

        Assert.Equal(new[] { 1, 2 }, result.Select(r => r.Id));
        Assert.True(result[1].IsSent);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_IncludesActivityTitle()
    {
        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Koşu", result!.ActivityTitle);
        Assert.Equal("Ayakkabı", result.Note);
        Assert.Equal(10, result.ActivityId);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Null(await CreateService().GetByIdAsync(99));
    }
}
