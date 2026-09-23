// ActivityService okuma senaryolarının birim testleridir.
// Repository, bellek içi bir fake ile değiştirilir.

using AjandaAI.Application.Activities;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Tests.Activities;

public class ActivityServiceTests
{
    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly List<Activity> _items;

        public FakeActivityRepository(params Activity[] items) => _items = items.ToList();

        public Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Activity>>(_items.ToList());

        public Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(a => a.Id == id));
    }

    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private static ActivityService CreateService() => new(new FakeActivityRepository(
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
        }));

    [Fact]
    public async Task GetAllAsync_ReturnsAllMappedDtos()
    {
        var result = await CreateService().GetAllAsync();

        Assert.Equal(new[] { 1, 2 }, result.Select(a => a.Id));
        Assert.Equal("Koşu", result[0].Title);
        Assert.Equal("Completed", result[0].Status);
        Assert.Equal("Medium", result[1].Priority);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDetailDto()
    {
        var result = await CreateService().GetByIdAsync(1);

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
        var result = await CreateService().GetByIdAsync(2);

        Assert.NotNull(result);
        Assert.Null(result!.Location);
        Assert.Null(result.Rating);
        Assert.Null(result.WouldRepeat);
        Assert.Equal(1500m, result.EstimatedBudget);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Null(await CreateService().GetByIdAsync(99));
    }
}
