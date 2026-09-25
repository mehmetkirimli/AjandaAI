// /api/activities uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden doğrular.

using System.Net;
using System.Net.Http.Json;
using AjandaAI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Activities;

public class ActivitiesEndpointTests : IntegrationTestBase
{
    public ActivitiesEndpointTests(AjandaApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_Valid_Returns201AndPersists()
    {
        var userId = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync("/api/activities", ActivityBody(userId, categoryId: 3), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadDataAsync(response)).GetProperty("id").GetInt32();
        Assert.Equal($"/api/activities/{id}", response.Headers.Location?.OriginalString);
        var activity = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal(userId, activity.UserId);
        Assert.Equal(3, activity.CategoryId);
    }

    [Fact]
    public async Task Post_NonExistingCategory_Returns400()
    {
        var userId = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync("/api/activities", ActivityBody(userId, categoryId: 9999), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InactiveUser_Returns400()
    {
        var userId = await CreateUserAsync();
        await Client.DeleteAsync($"/api/users/{userId}");

        var response = await Client.PostAsJsonAsync("/api/activities", ActivityBody(userId), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EndBeforeStart_Returns400()
    {
        var userId = await CreateUserAsync();
        var start = DateTimeOffset.UtcNow.AddDays(3);

        var response = await Client.PostAsJsonAsync("/api/activities",
            ActivityBody(userId, start: start, end: start.AddHours(-1)), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActive()
    {
        var userId = await CreateUserAsync();
        var activeId = await CreateActivityAsync(userId);
        var deletedId = await CreateActivityAsync(userId);
        await Client.DeleteAsync($"/api/activities/{deletedId}");

        var ids = (await ReadDataAsync(await Client.GetAsync("/api/activities?pageSize=100")))
            .GetProperty("items").EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(deletedId, ids);
    }

    // Sınıf içindeki testler aynı tabloyu paylaştığı için her test kendi tarih penceresinde çalışır.
    private async Task CreateActivitiesAtAsync(int userId, IEnumerable<DateTimeOffset> starts)
    {
        foreach (var start in starts)
        {
            var response = await Client.PostAsJsonAsync("/api/activities", ActivityBody(userId, start: start), Json);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    private static string Iso(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("o"));

    [Fact]
    public async Task GetAll_25Activities_Page1Has20_Page2Has5()
    {
        var userId = await CreateUserAsync();
        var from = new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await CreateActivitiesAtAsync(userId, Enumerable.Range(0, 25).Select(i => from.AddHours(i)));
        var range = $"from={Iso(from)}&to={Iso(from.AddDays(2))}";

        var page1 = await ReadDataAsync(await Client.GetAsync($"/api/activities?{range}&page=1"));
        var page2 = await ReadDataAsync(await Client.GetAsync($"/api/activities?{range}&page=2"));

        Assert.Equal(20, page1.GetProperty("items").GetArrayLength());
        Assert.Equal(25, page1.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page1.GetProperty("totalPages").GetInt32());
        Assert.True(page1.GetProperty("hasNext").GetBoolean());
        Assert.False(page1.GetProperty("hasPrevious").GetBoolean());

        Assert.Equal(5, page2.GetProperty("items").GetArrayLength());
        Assert.False(page2.GetProperty("hasNext").GetBoolean());
        Assert.True(page2.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task GetAll_DateRange_ReturnsOnlyActivitiesStartingInRange()
    {
        var userId = await CreateUserAsync();
        var baseTime = new DateTimeOffset(2091, 6, 1, 0, 0, 0, TimeSpan.Zero);
        await CreateActivitiesAtAsync(userId, new[] { baseTime, baseTime.AddDays(5), baseTime.AddDays(10) });

        var data = await ReadDataAsync(await Client.GetAsync(
            $"/api/activities?from={Iso(baseTime.AddDays(1))}&to={Iso(baseTime.AddDays(10))}"));

        var starts = data.GetProperty("items").EnumerateArray()
            .Select(a => a.GetProperty("start").GetDateTimeOffset()).ToList();
        Assert.Equal(new[] { baseTime.AddDays(5), baseTime.AddDays(10) }, starts);
    }

    [Fact]
    public async Task GetAll_FromAfterTo_Returns400()
    {
        var from = new DateTimeOffset(2092, 1, 2, 0, 0, 0, TimeSpan.Zero);

        var response = await Client.GetAsync($"/api/activities?from={Iso(from)}&to={Iso(from.AddDays(-1))}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Başlangıç tarihi bitişten sonra olamaz.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetAll_PageSize500_IsClampedTo100()
    {
        var data = await ReadDataAsync(await Client.GetAsync("/api/activities?pageSize=500&page=0"));

        Assert.Equal(100, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, data.GetProperty("page").GetInt32());
    }
}
