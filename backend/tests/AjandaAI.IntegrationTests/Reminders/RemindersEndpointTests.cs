// /api/reminders uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden, token'lı isteklerle doğrular.
// Sahiplik: Reminder'ın sahibi bağlı aktivitenin sahibidir. AUTH-45 ve AUTH-41..46'nın Reminder karşılığı
// (docs/auth-test-senaryolari.md, ADR 0018 Karar 3). Her test kendi kullanıcılarını kaydeder/doğrular/login eder.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Reminders;

public class RemindersEndpointTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string UnauthorizedTemplate = "Yetkisiz erişim denemesi {UserId} {ReminderId}";
    private const string ActivityMissingMessage = "Aktivite bulunamadı veya silinmiş.";

    public RemindersEndpointTests(AuthApiFactory factory) : base(factory) { }

    private sealed record TestUser(int Id, string Token);

    private async Task<TestUser> NewUserAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        return new TestUser(id, access);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return Client.SendAsync(request);
    }

    // Aktivite 7 gün sonra başlar; hatırlatma ondan önce, şimdiden sonra olmalıdır.
    private static object ReminderBody(int activityId, string note = "Hatırlat") => new
    {
        ActivityId = activityId,
        RemindAt = DateTimeOffset.UtcNow.AddDays(1),
        Note = note
    };

    // Aktivite uç noktası bu testlerin konusu değil; aktivite sahibiyle birlikte DB'ye doğrudan eklenir.
    private Task<int> CreateActivityAsync(int userId)
    {
        var now = DateTimeOffset.UtcNow;
        var start = now.AddDays(7);
        return Factory.WithDbAsync(async db =>
        {
            var activity = new Activity
            {
                UserId = userId, CategoryId = 1, Title = "Entegrasyon aktivitesi", Description = "Açıklama",
                Status = ActivityStatus.Planned, Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low,
                Start = start, End = start.AddHours(2), EstimatedBudget = 100m, CreatedAt = now, UpdatedAt = now
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return activity.Id;
        });
    }

    private Task DeactivateActivityAsync(int activityId) =>
        Factory.WithDbAsync(db => db.Activities.Where(a => a.Id == activityId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false)));

    private async Task<int> CreateReminderForAsync(TestUser user, string note = "Hatırlat")
    {
        var activityId = await CreateActivityAsync(user.Id);
        var response = await SendAsync(HttpMethod.Post, "/api/reminders", user.Token, ReminderBody(activityId, note));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }

    private IEnumerable<LogEvent> UnauthorizedLogsFor(int reminderId) =>
        Factory.Logs.Events.Where(e =>
            e.MessageTemplate.Text == UnauthorizedTemplate &&
            e.Properties.TryGetValue("ReminderId", out var v) && v is ScalarValue { Value: int id } && id == reminderId);

    // AUTH-40 (Reminder controller artık token ister; geçici [AllowAnonymous] kaldırıldı)
    [Theory]
    [InlineData("GET", "/api/reminders")]
    [InlineData("GET", "/api/reminders/1")]
    [InlineData("POST", "/api/reminders")]
    [InlineData("PUT", "/api/reminders/1")]
    [InlineData("DELETE", "/api/reminders/1")]
    public async Task AUTH_40_ReminderEndpoints_WithoutToken_Return401(string method, string path)
    {
        var body = method is "POST" or "PUT" ? ReminderBody(1) : null;

        var response = await SendAsync(new HttpMethod(method), path, token: null, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_InactiveActivity_Returns400()
    {
        var user = await NewUserAsync();
        var activityId = await CreateActivityAsync(user.Id);
        await DeactivateActivityAsync(activityId);

        var response = await SendAsync(HttpMethod.Post, "/api/reminders", user.Token, ReminderBody(activityId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_OwnActivity_Returns201AndPersists()
    {
        var user = await NewUserAsync();
        var activityId = await CreateActivityAsync(user.Id);

        var response = await SendAsync(HttpMethod.Post, "/api/reminders", user.Token, ReminderBody(activityId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadDataAsync(response)).GetProperty("id").GetInt32();
        var stored = await Factory.WithDbAsync(db => db.Reminders.AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal(activityId, stored.ActivityId);
    }

    // AUTH-45
    [Fact]
    public async Task AUTH_45_Post_OtherUsersActivity_Returns400_NothingCreated_SameBodyAsMissingActivity()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var bActivityId = await CreateActivityAsync(b.Id);
        const int missingActivityId = 987654;

        var foreign = await SendAsync(HttpMethod.Post, "/api/reminders", a.Token, ReminderBody(bActivityId));
        var missing = await SendAsync(HttpMethod.Post, "/api/reminders", a.Token, ReminderBody(missingActivityId));

        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains(ActivityMissingMessage, await foreign.Content.ReadAsStringAsync());
        // Hata gövdesi aktivite var olsa da olmasa da aynıdır (varlık sızmaz).
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await foreign.Content.ReadAsStringAsync());
        Assert.False(await Factory.WithDbAsync(db => db.Reminders.AnyAsync(r => r.ActivityId == bActivityId)));
    }

    // AUTH-45 (güncellemede ActivityId'yi başkasının aktivitesine çevirmek)
    [Fact]
    public async Task AUTH_45_Put_MoveToOtherUsersActivity_Returns400_AndRecordUnchanged()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateReminderForAsync(a);
        var before = await Factory.WithDbAsync(db => db.Reminders.AsNoTracking().SingleAsync(r => r.Id == id));
        var bActivityId = await CreateActivityAsync(b.Id);

        var response = await SendAsync(HttpMethod.Put, $"/api/reminders/{id}", a.Token, ReminderBody(bActivityId, "taşındı"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var after = await Factory.WithDbAsync(db => db.Reminders.AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal(before.ActivityId, after.ActivityId);
        Assert.Equal(before.Note, after.Note);
    }

    // AUTH-46 (Reminder)
    [Fact]
    public async Task AUTH_46_GetAll_ReturnsOnlyOwnReminders()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var aIds = new[] { await CreateReminderForAsync(a), await CreateReminderForAsync(a) };
        var bId = await CreateReminderForAsync(b);

        var aList = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/reminders?pageSize=100", a.Token));
        var bList = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/reminders?pageSize=100", b.Token));

        var aReturned = aList.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList();
        var bReturned = bList.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(aIds.OrderBy(i => i), aReturned.OrderBy(i => i));
        Assert.DoesNotContain(bId, aReturned);
        Assert.Equal(new[] { bId }, bReturned);
        Assert.Equal(2, aList.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, bList.GetProperty("totalCount").GetInt32());
    }

    // AUTH-41 (Reminder)
    [Fact]
    public async Task AUTH_41_Get_OtherUsersReminder_Returns404_WithBodyByteIdenticalToRealNotFound()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var bReminderId = await CreateReminderForAsync(b);

        var foreign = await SendAsync(HttpMethod.Get, $"/api/reminders/{bReminderId}", a.Token);
        var foreignBody = await foreign.Content.ReadAsStringAsync();
        // Aynı Id gerçekten yok olsun (hard delete) ve aynı istek tekrarlansın: bu gerçek 404'tür.
        await Factory.WithDbAsync(db => db.Reminders.Where(x => x.Id == bReminderId).ExecuteDeleteAsync());
        var real = await SendAsync(HttpMethod.Get, $"/api/reminders/{bReminderId}", a.Token);
        var realBody = await real.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, real.StatusCode);
        Assert.Equal(realBody, foreignBody);
    }

    // AUTH-41 (Reminder; sahibi kendi kaydını okuyabilir)
    [Fact]
    public async Task AUTH_41_Get_OwnReminder_Returns200()
    {
        var a = await NewUserAsync();
        var id = await CreateReminderForAsync(a);

        var response = await SendAsync(HttpMethod.Get, $"/api/reminders/{id}", a.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, (await ReadDataAsync(response)).GetProperty("id").GetInt32());
    }

    // AUTH-42 (Reminder)
    [Fact]
    public async Task AUTH_42_Put_OtherUsersReminder_Returns404_AndRecordIsUnchanged()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateReminderForAsync(b, "B'nin notu");
        var aActivityId = await CreateActivityAsync(a.Id);

        var response = await SendAsync(HttpMethod.Put, $"/api/reminders/{id}", a.Token, ReminderBody(aActivityId, "A ele geçirdi"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var after = await Factory.WithDbAsync(db => db.Reminders.Include(r => r.Activity).AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal("B'nin notu", after.Note);
        Assert.Equal(b.Id, after.Activity.UserId);
        Assert.NotEqual(aActivityId, after.ActivityId);
    }

    // AUTH-42 (Reminder)
    [Fact]
    public async Task AUTH_42_Delete_OtherUsersReminder_Returns404_AndRowStays()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateReminderForAsync(b);

        var response = await SendAsync(HttpMethod.Delete, $"/api/reminders/{id}", a.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(await Factory.WithDbAsync(db => db.Reminders.AnyAsync(r => r.Id == id)));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/reminders/{id}", b.Token)).StatusCode);
    }

    // AUTH-42 (Reminder; sahibi kendi kaydını güncelleyebilir ve hard delete edebilir)
    [Fact]
    public async Task AUTH_42_Put_And_Delete_OwnReminder_Succeed_HardDeletes()
    {
        var a = await NewUserAsync();
        var id = await CreateReminderForAsync(a);
        var activityId = await Factory.WithDbAsync(async db => (await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == id)).ActivityId);

        var put = await SendAsync(HttpMethod.Put, $"/api/reminders/{id}", a.Token, ReminderBody(activityId, "Yeni not"));
        var delete = await SendAsync(HttpMethod.Delete, $"/api/reminders/{id}", a.Token);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Yeni not", (await ReadDataAsync(put)).GetProperty("note").GetString());
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.False(await Factory.WithDbAsync(db => db.Reminders.AnyAsync(r => r.Id == id)));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/reminders/{id}", a.Token)).StatusCode);
    }

    // AUTH-43 (Reminder)
    [Fact]
    public async Task AUTH_43_OtherUsersReminder_WritesUnauthorizedAccessWarning_ForGetPutDelete()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateReminderForAsync(b);
        var aActivityId = await CreateActivityAsync(a.Id);

        await SendAsync(HttpMethod.Get, $"/api/reminders/{id}", a.Token);
        await SendAsync(HttpMethod.Put, $"/api/reminders/{id}", a.Token, ReminderBody(aActivityId));
        await SendAsync(HttpMethod.Delete, $"/api/reminders/{id}", a.Token);

        var logs = UnauthorizedLogsFor(id).ToList();
        Assert.Equal(3, logs.Count);
        Assert.All(logs, e =>
        {
            Assert.Equal(LogEventLevel.Warning, e.Level);
            Assert.Equal(a.Id, (int)((ScalarValue)e.Properties["UserId"]).Value!);
        });
    }

    // AUTH-43 (Reminder; sahibin kendi erişimi uyarı üretmez)
    [Fact]
    public async Task AUTH_43_OwnReminderAccess_WritesNoWarning()
    {
        var a = await NewUserAsync();
        var id = await CreateReminderForAsync(a);

        await SendAsync(HttpMethod.Get, $"/api/reminders/{id}", a.Token);

        Assert.Empty(UnauthorizedLogsFor(id));
    }

    // AUTH-44 (Reminder)
    [Fact]
    public async Task AUTH_44_NonExistingId_Returns404_WithoutWarningLog()
    {
        var a = await NewUserAsync();
        const int missingId = 987654;
        var aActivityId = await CreateActivityAsync(a.Id);

        var get = await SendAsync(HttpMethod.Get, $"/api/reminders/{missingId}", a.Token);
        var put = await SendAsync(HttpMethod.Put, $"/api/reminders/{missingId}", a.Token, ReminderBody(aActivityId));
        var delete = await SendAsync(HttpMethod.Delete, $"/api/reminders/{missingId}", a.Token);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(UnauthorizedLogsFor(missingId));
        Assert.DoesNotContain(Factory.Logs.Events, e =>
            e.MessageTemplate.Text == UnauthorizedTemplate &&
            e.Properties.TryGetValue("UserId", out var v) && v is ScalarValue { Value: int uid } && uid == a.Id);
    }
}
