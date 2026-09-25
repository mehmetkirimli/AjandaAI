// /api/reminders uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden doğrular.

using System.Net;
using System.Net.Http.Json;
using AjandaAI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Reminders;

public class RemindersEndpointTests : IntegrationTestBase
{
    public RemindersEndpointTests(AjandaApiFactory factory) : base(factory) { }

    // Aktivite 7 gün sonra başlar; hatırlatma ondan önce, şimdiden sonra olmalıdır.
    private static object ReminderBody(int activityId) => new
    {
        ActivityId = activityId,
        RemindAt = DateTimeOffset.UtcNow.AddDays(1),
        Note = "Hatırlat"
    };

    [Fact]
    public async Task Post_InactiveActivity_Returns400()
    {
        var activityId = await CreateActivityAsync(await CreateUserAsync());
        await Client.DeleteAsync($"/api/activities/{activityId}");

        var response = await Client.PostAsJsonAsync("/api/reminders", ReminderBody(activityId), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_HardDeletes_RowIsGone()
    {
        var activityId = await CreateActivityAsync(await CreateUserAsync());
        var created = await Client.PostAsJsonAsync("/api/reminders", ReminderBody(activityId), Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadDataAsync(created)).GetProperty("id").GetInt32();

        var response = await Client.DeleteAsync($"/api/reminders/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await Factory.WithDbAsync(db => db.Reminders.AnyAsync(r => r.Id == id)));
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/reminders/{id}")).StatusCode);
    }
}
