// /api/activities uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden, token'lı isteklerle doğrular.
// Sahiplik (IDOR) senaryoları: AUTH-41, 42, 43, 44, 46, 47 (docs/auth-test-senaryolari.md, ADR 0018 Karar 3).
// Her test kendi kullanıcılarını kaydeder/doğrular/login eder; böylece listeler testler arasında karışmaz.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Activities;

public class ActivitiesEndpointTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string UnauthorizedTemplate = "Yetkisiz erişim denemesi {UserId} {ActivityId}";

    public ActivitiesEndpointTests(AuthApiFactory factory) : base(factory) { }

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

    private static object ActivityBody(int categoryId = 1, DateTimeOffset? start = null, DateTimeOffset? end = null,
        string title = "Entegrasyon aktivitesi")
    {
        var s = start ?? DateTimeOffset.UtcNow.AddDays(7);
        return new
        {
            CategoryId = categoryId,
            Title = title,
            Description = "Açıklama",
            Status = "Planned",
            Priority = "Medium",
            EnergyLevel = "Low",
            Start = s,
            End = end ?? s.AddHours(2),
            IsAllDay = false,
            Location = (string?)null,
            IsFlexible = false,
            EstimatedBudget = 100m,
            Rating = (int?)null,
            WouldRepeat = (bool?)null
        };
    }

    private async Task<int> CreateActivityForAsync(TestUser user, DateTimeOffset? start = null, string title = "Entegrasyon aktivitesi")
    {
        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, ActivityBody(start: start, title: title));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }

    private static string Iso(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("o"));

    private IEnumerable<LogEvent> UnauthorizedLogsFor(int activityId) =>
        Factory.Logs.Events.Where(e =>
            e.MessageTemplate.Text == UnauthorizedTemplate &&
            e.Properties.TryGetValue("ActivityId", out var v) && v is ScalarValue { Value: int id } && id == activityId);

    // AUTH-40 (Activity controller artık token ister; geçici [AllowAnonymous] kaldırıldı)
    [Theory]
    [InlineData("GET", "/api/activities")]
    [InlineData("GET", "/api/activities/1")]
    [InlineData("POST", "/api/activities")]
    [InlineData("PUT", "/api/activities/1")]
    [InlineData("DELETE", "/api/activities/1")]
    public async Task AUTH_40_ActivityEndpoints_WithoutToken_Return401(string method, string path)
    {
        var body = method is "POST" or "PUT" ? ActivityBody() : null;

        var response = await SendAsync(new HttpMethod(method), path, token: null, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Valid_Returns201AndPersists()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, ActivityBody(categoryId: 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadDataAsync(response)).GetProperty("id").GetInt32();
        Assert.Equal($"/api/activities/{id}", response.Headers.Location?.OriginalString);
        var activity = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal(user.Id, activity.UserId);
        Assert.Equal(3, activity.CategoryId);
    }

    // AUTH-47
    [Fact]
    public async Task AUTH_47_Post_BodyWithOtherUserId_IsIgnored_OwnerIsTokenUser()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var body = new Dictionary<string, object?>
        {
            ["UserId"] = b.Id,
            ["CategoryId"] = 1,
            ["Title"] = "Sahte sahip denemesi",
            ["Description"] = "x",
            ["Status"] = "Planned",
            ["Priority"] = "Low",
            ["EnergyLevel"] = "Low",
            ["Start"] = DateTimeOffset.UtcNow.AddDays(3),
            ["End"] = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
            ["IsAllDay"] = false,
            ["IsFlexible"] = false,
            ["EstimatedBudget"] = 0m
        };

        var response = await SendAsync(HttpMethod.Post, "/api/activities", a.Token, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        Assert.Equal(a.Id, data.GetProperty("userId").GetInt32());
        var id = data.GetProperty("id").GetInt32();
        var stored = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(x => x.Id == id));
        Assert.Equal(a.Id, stored.UserId);
        Assert.NotEqual(b.Id, stored.UserId);
        Assert.Equal(0, await Factory.WithDbAsync(db => db.Activities.CountAsync(x => x.UserId == b.Id)));
    }

    [Fact]
    public async Task Post_NonExistingCategory_Returns400()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, ActivityBody(categoryId: 9999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EndBeforeStart_Returns400()
    {
        var user = await NewUserAsync();
        var start = DateTimeOffset.UtcNow.AddDays(3);

        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token,
            ActivityBody(start: start, end: start.AddHours(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActive()
    {
        var user = await NewUserAsync();
        var activeId = await CreateActivityForAsync(user);
        var deletedId = await CreateActivityForAsync(user);
        await SendAsync(HttpMethod.Delete, $"/api/activities/{deletedId}", user.Token);

        var ids = (await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/activities?pageSize=100", user.Token)))
            .GetProperty("items").EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(deletedId, ids);
    }

    // AUTH-46
    [Fact]
    public async Task AUTH_46_GetAll_ReturnsOnlyOwnActivities()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var aIds = new[] { await CreateActivityForAsync(a), await CreateActivityForAsync(a) };
        var bId = await CreateActivityForAsync(b);

        var aList = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/activities?pageSize=100", a.Token));
        var bList = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/activities?pageSize=100", b.Token));

        var aReturned = aList.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList();
        var bReturned = bList.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(aIds.OrderBy(i => i), aReturned.OrderBy(i => i));
        Assert.DoesNotContain(bId, aReturned);
        Assert.Equal(new[] { bId }, bReturned);
        Assert.Equal(2, aList.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, bList.GetProperty("totalCount").GetInt32());
    }

    // AUTH-41
    [Fact]
    public async Task AUTH_41_Get_OtherUsersActivity_Returns404_WithBodyByteIdenticalToRealNotFound()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var bActivityId = await CreateActivityForAsync(b);

        var foreign = await SendAsync(HttpMethod.Get, $"/api/activities/{bActivityId}", a.Token);
        var foreignBody = await foreign.Content.ReadAsStringAsync();
        // Aynı Id gerçekten yok olsun (hard delete) ve aynı istek tekrarlansın: bu gerçek 404'tür.
        await Factory.WithDbAsync(db => db.Activities.Where(x => x.Id == bActivityId).ExecuteDeleteAsync());
        var real = await SendAsync(HttpMethod.Get, $"/api/activities/{bActivityId}", a.Token);
        var realBody = await real.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, real.StatusCode);
        Assert.Equal(realBody, foreignBody);
    }

    // AUTH-41 (sahibi kendi kaydını okuyabilir: sorgu sahibe 404 vermez)
    [Fact]
    public async Task AUTH_41_Get_OwnActivity_Returns200()
    {
        var a = await NewUserAsync();
        var id = await CreateActivityForAsync(a);

        var response = await SendAsync(HttpMethod.Get, $"/api/activities/{id}", a.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, (await ReadDataAsync(response)).GetProperty("id").GetInt32());
    }

    // AUTH-42
    [Fact]
    public async Task AUTH_42_Put_OtherUsersActivity_Returns404_AndRecordIsUnchanged()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateActivityForAsync(b, title: "B'nin planı");
        var before = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(x => x.Id == id));

        var response = await SendAsync(HttpMethod.Put, $"/api/activities/{id}", a.Token,
            ActivityBody(categoryId: 3, title: "A ele geçirdi"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var after = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(x => x.Id == id));
        Assert.Equal("B'nin planı", after.Title);
        Assert.Equal(before.CategoryId, after.CategoryId);
        Assert.Equal(b.Id, after.UserId);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
    }

    // AUTH-42
    [Fact]
    public async Task AUTH_42_Delete_OtherUsersActivity_Returns404_AndRecordStaysActive()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateActivityForAsync(b);

        var response = await SendAsync(HttpMethod.Delete, $"/api/activities/{id}", a.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var after = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(x => x.Id == id));
        Assert.True(after.IsActive);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/activities/{id}", b.Token)).StatusCode);
    }

    // AUTH-42 (sahibi kendi kaydını güncelleyebilir ve silebilir)
    [Fact]
    public async Task AUTH_42_Put_And_Delete_OwnActivity_Succeed()
    {
        var a = await NewUserAsync();
        var id = await CreateActivityForAsync(a);

        var put = await SendAsync(HttpMethod.Put, $"/api/activities/{id}", a.Token, ActivityBody(title: "Yeni başlık"));
        var delete = await SendAsync(HttpMethod.Delete, $"/api/activities/{id}", a.Token);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Yeni başlık", (await ReadDataAsync(put)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.False((await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(x => x.Id == id))).IsActive);
    }

    // AUTH-43
    [Fact]
    public async Task AUTH_43_OtherUsersActivity_WritesUnauthorizedAccessWarning_ForGetPutDelete()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var id = await CreateActivityForAsync(b);

        await SendAsync(HttpMethod.Get, $"/api/activities/{id}", a.Token);
        await SendAsync(HttpMethod.Put, $"/api/activities/{id}", a.Token, ActivityBody());
        await SendAsync(HttpMethod.Delete, $"/api/activities/{id}", a.Token);

        var logs = UnauthorizedLogsFor(id).ToList();
        Assert.Equal(3, logs.Count);
        Assert.All(logs, e =>
        {
            Assert.Equal(LogEventLevel.Warning, e.Level);
            Assert.Equal(a.Id, (int)((ScalarValue)e.Properties["UserId"]).Value!);
        });
    }

    // AUTH-43 (sahibin kendi erişimi uyarı üretmez)
    [Fact]
    public async Task AUTH_43_OwnActivityAccess_WritesNoWarning()
    {
        var a = await NewUserAsync();
        var id = await CreateActivityForAsync(a);

        await SendAsync(HttpMethod.Get, $"/api/activities/{id}", a.Token);

        Assert.Empty(UnauthorizedLogsFor(id));
    }

    // AUTH-44
    [Fact]
    public async Task AUTH_44_NonExistingId_Returns404_WithoutWarningLog()
    {
        var a = await NewUserAsync();
        const int missingId = 987654;

        var get = await SendAsync(HttpMethod.Get, $"/api/activities/{missingId}", a.Token);
        var put = await SendAsync(HttpMethod.Put, $"/api/activities/{missingId}", a.Token, ActivityBody());
        var delete = await SendAsync(HttpMethod.Delete, $"/api/activities/{missingId}", a.Token);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(UnauthorizedLogsFor(missingId));
        Assert.DoesNotContain(Factory.Logs.Events, e =>
            e.MessageTemplate.Text == UnauthorizedTemplate &&
            e.Properties.TryGetValue("UserId", out var v) && v is ScalarValue { Value: int uid } && uid == a.Id);
    }

    // Sınıf içindeki testler aynı tabloyu paylaştığı için her test kendi kullanıcısıyla çalışır.
    private async Task CreateActivitiesAtAsync(TestUser user, IEnumerable<DateTimeOffset> starts)
    {
        foreach (var start in starts)
            await CreateActivityForAsync(user, start);
    }

    [Fact]
    public async Task GetAll_25Activities_Page1Has20_Page2Has5()
    {
        var user = await NewUserAsync();
        var from = new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await CreateActivitiesAtAsync(user, Enumerable.Range(0, 25).Select(i => from.AddHours(i)));
        var range = $"from={Iso(from)}&to={Iso(from.AddDays(2))}";

        var page1 = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/activities?{range}&page=1", user.Token));
        var page2 = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/activities?{range}&page=2", user.Token));

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
        var user = await NewUserAsync();
        var baseTime = new DateTimeOffset(2091, 6, 1, 0, 0, 0, TimeSpan.Zero);
        await CreateActivitiesAtAsync(user, new[] { baseTime, baseTime.AddDays(5), baseTime.AddDays(10) });

        var data = await ReadDataAsync(await SendAsync(HttpMethod.Get,
            $"/api/activities?from={Iso(baseTime.AddDays(1))}&to={Iso(baseTime.AddDays(10))}", user.Token));

        var starts = data.GetProperty("items").EnumerateArray()
            .Select(a => a.GetProperty("start").GetDateTimeOffset()).ToList();
        Assert.Equal(new[] { baseTime.AddDays(5), baseTime.AddDays(10) }, starts);
    }

    [Fact]
    public async Task GetAll_FromAfterTo_Returns400()
    {
        var user = await NewUserAsync();
        var from = new DateTimeOffset(2092, 1, 2, 0, 0, 0, TimeSpan.Zero);

        var response = await SendAsync(HttpMethod.Get,
            $"/api/activities?from={Iso(from)}&to={Iso(from.AddDays(-1))}", user.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Başlangıç tarihi bitişten sonra olamaz.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetAll_PageSize500_IsClampedTo100()
    {
        var user = await NewUserAsync();

        var data = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/activities?pageSize=500&page=0", user.Token));

        Assert.Equal(100, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, data.GetProperty("page").GetInt32());
    }
}
