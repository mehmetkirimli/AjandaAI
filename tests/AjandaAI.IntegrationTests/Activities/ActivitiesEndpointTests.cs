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

        var ids = (await ReadDataAsync(await Client.GetAsync("/api/activities")))
            .EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(deletedId, ids);
    }
}
