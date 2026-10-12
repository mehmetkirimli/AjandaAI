// /api/activities/series uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden doğrular (ADR 0022).
// Senaryolar: SERIES-01, 05, 08, 09, 10, 11, 12, 13, 20, 21, 22, 23, 24 (docs/seri-test-senaryolari.md).
// SERIES-02, 03, 04, 06, 07 saf üretici mantığıdır; birim testlerindedir (RecurrenceExpanderTests).
// Kayıtlı kullanıcıların saat dilimi Europe/Istanbul'dur (UTC+3, yaz saati yok); tarihler bu varsayıma göre seçilir.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Activities;

public class ActivitySeriesTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    public ActivitySeriesTests(AuthApiFactory factory) : base(factory) { }

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

    private static Dictionary<string, object?> SeriesBody(
        DateTimeOffset start, string frequency, string until, int[]? days = null,
        int? reminder = null, int categoryId = 1, string title = "Krem sür", TimeSpan? length = null)
    {
        return new Dictionary<string, object?>
        {
            ["CategoryId"] = categoryId,
            ["Title"] = title,
            ["Description"] = "Her akşam",
            ["Status"] = "Planned",
            ["Priority"] = "Medium",
            ["EnergyLevel"] = "Low",
            ["Start"] = start,
            ["End"] = start + (length ?? TimeSpan.FromMinutes(10)),
            ["IsAllDay"] = false,
            ["IsFlexible"] = false,
            ["EstimatedBudget"] = 0m,
            ["Recurrence"] = new Dictionary<string, object?>
            {
                ["Frequency"] = frequency,
                ["DaysOfWeek"] = days ?? Array.Empty<int>(),
                ["Until"] = until
            },
            ["ReminderMinutesBefore"] = reminder
        };
    }

    private async Task<int> CreateSeriesAsync(TestUser user, object body, int expectedCount)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        Assert.Equal(expectedCount, data.GetProperty("createdCount").GetInt32());
        return data.GetProperty("seriesId").GetInt32();
    }

    private Task<int> CountActivitiesAsync(int userId) =>
        Factory.WithDbAsync(db => db.Activities.CountAsync(a => a.UserId == userId));

    private static async Task<List<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadRootAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).ToList();

    // SERIES-01 + SERIES-12 (yanıt alanları)
    [Fact]
    public async Task SERIES_01_Daily_six_months_creates_183_activities_in_one_series()
    {
        var user = await NewUserAsync();
        var start = new DateTimeOffset(2026, 10, 12, 21, 0, 0, Istanbul);

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token,
            SeriesBody(start, "Daily", "2027-04-12"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        var seriesId = data.GetProperty("seriesId").GetInt32();
        Assert.Equal(183, data.GetProperty("createdCount").GetInt32());

        var activities = await Factory.WithDbAsync(db =>
            db.Activities.AsNoTracking().Where(a => a.UserId == user.Id).OrderBy(a => a.Start).ToListAsync());
        Assert.Equal(183, activities.Count);
        Assert.All(activities, a =>
        {
            Assert.Equal(seriesId, a.SeriesId);
            var local = a.Start.ToOffset(Istanbul);
            Assert.Equal(new TimeSpan(21, 0, 0), local.TimeOfDay);
            Assert.Equal(TimeSpan.FromMinutes(10), a.End - a.Start);
        });

        // SERIES-12
        Assert.Equal(start, data.GetProperty("firstStart").GetDateTimeOffset());
        Assert.Equal(new DateTimeOffset(2027, 4, 12, 21, 0, 0, Istanbul), data.GetProperty("lastStart").GetDateTimeOffset());
        var series = await Factory.WithDbAsync(db => db.ActivitySeries.AsNoTracking().SingleAsync(s => s.Id == seriesId));
        Assert.Equal(user.Id, series.UserId);
        Assert.Equal(new DateOnly(2026, 10, 12), series.StartDate);
        Assert.Equal(new DateOnly(2027, 4, 12), series.EndDate);
    }

    // SERIES-05
    [Fact]
    public async Task SERIES_05_Weekly_without_days_returns_400_and_creates_nothing()
    {
        var user = await NewUserAsync();
        var body = SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Weekly", "2030-05-20", days: Array.Empty<int>());

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Haftalık tekrar için en az bir gün seçilmelidir.", await ErrorsAsync(response));
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // SERIES-08
    [Fact]
    public async Task SERIES_08_More_than_366_occurrences_returns_400_and_creates_nothing()
    {
        var user = await NewUserAsync();
        var body = SeriesBody(new DateTimeOffset(2030, 1, 1, 21, 0, 0, Istanbul), "Daily", "2031-01-02");

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Bir seri en fazla 366 aktivite içerebilir.", await ErrorsAsync(response));
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
        Assert.False(await Factory.WithDbAsync(db => db.ActivitySeries.AnyAsync(s => s.UserId == user.Id)));
    }

    // SERIES-09
    [Fact]
    public async Task SERIES_09_Until_before_first_day_returns_400()
    {
        var user = await NewUserAsync();
        var body = SeriesBody(new DateTimeOffset(2030, 5, 10, 21, 0, 0, Istanbul), "Daily", "2030-05-09");

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Bitiş tarihi başlangıç gününden önce olamaz.", await ErrorsAsync(response));
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // SERIES-10
    [Fact]
    public async Task SERIES_10_Invalid_template_returns_400_with_single_activity_messages()
    {
        var user = await NewUserAsync();
        var start = new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul);

        var emptyTitle = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token,
            SeriesBody(start, "Daily", "2030-05-03", title: ""));
        var badCategory = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token,
            SeriesBody(start, "Daily", "2030-05-03", categoryId: 999_999));

        Assert.Equal(HttpStatusCode.BadRequest, emptyTitle.StatusCode);
        Assert.Contains("Başlık zorunludur.", await ErrorsAsync(emptyTitle));
        Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
        Assert.Contains("Kategori bulunamadı veya aktif değil.", await ErrorsAsync(badCategory));
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
        Assert.False(await Factory.WithDbAsync(db => db.ActivitySeries.AnyAsync(s => s.UserId == user.Id)));
    }

    // SERIES-11
    [Fact]
    public async Task SERIES_11_Reminder_is_added_to_future_occurrences_and_skipped_for_past_ones()
    {
        var user = await NewUserAsync();
        // İlk oluşum 10 dk önce: 15 dk öncesi hatırlatma geçmişte kalır. Sonraki iki gün gelecektedir.
        var start = DateTimeOffset.UtcNow.AddMinutes(-10);
        var seriesId = await CreateSeriesAsync(user, SeriesBody(start, "Daily", DateOnly.FromDateTime(start.ToOffset(Istanbul).DateTime).AddDays(2).ToString("yyyy-MM-dd"), reminder: 15), 3);

        var activities = await Factory.WithDbAsync(db => db.Activities.AsNoTracking()
            .Include(a => a.Reminders).Where(a => a.SeriesId == seriesId).OrderBy(a => a.Start).ToListAsync());

        Assert.Equal(3, activities.Count);
        Assert.Empty(activities[0].Reminders);
        foreach (var activity in activities.Skip(1))
        {
            var reminder = Assert.Single(activity.Reminders);
            Assert.Equal(activity.Start.AddMinutes(-15), reminder.RemindAt);
        }
    }

    [Fact]
    public async Task Reminder_minutes_out_of_range_returns_400()
    {
        var user = await NewUserAsync();
        var body = SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-03", reminder: 10081);

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // SERIES-13
    [Fact]
    public async Task SERIES_13_Body_UserId_is_ignored_owner_is_token_user()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var body = SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-03");
        body["UserId"] = b.Id;

        var seriesId = await CreateSeriesAsync(a, body, 3);

        var series = await Factory.WithDbAsync(db => db.ActivitySeries.AsNoTracking().SingleAsync(s => s.Id == seriesId));
        Assert.Equal(a.Id, series.UserId);
        Assert.Equal(3, await CountActivitiesAsync(a.Id));
        Assert.Equal(0, await CountActivitiesAsync(b.Id));
    }

    // SERIES-20
    [Fact]
    public async Task SERIES_20_Detail_has_series_summary_for_series_activity_and_null_otherwise()
    {
        var user = await NewUserAsync();
        var start = new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul);
        var seriesId = await CreateSeriesAsync(user,
            SeriesBody(start, "Weekly", "2030-05-14", days: new[] { 3, 5 }), 4);
        var inSeries = await Factory.WithDbAsync(db =>
            db.Activities.AsNoTracking().Where(a => a.SeriesId == seriesId).Select(a => a.Id).FirstAsync());
        var single = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, new Dictionary<string, object?>
        {
            ["CategoryId"] = 1, ["Title"] = "Tekil", ["Description"] = "x", ["Status"] = "Planned", ["Priority"] = "Low",
            ["EnergyLevel"] = "Low", ["Start"] = start, ["End"] = start.AddHours(1), ["IsAllDay"] = false,
            ["IsFlexible"] = false, ["EstimatedBudget"] = 0m
        });
        var singleId = (await ReadDataAsync(single)).GetProperty("id").GetInt32();

        var seriesDetail = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/activities/{inSeries}", user.Token));
        var singleDetail = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/activities/{singleId}", user.Token));

        var summary = seriesDetail.GetProperty("series");
        Assert.Equal(seriesId, summary.GetProperty("id").GetInt32());
        Assert.Equal("Weekly", summary.GetProperty("frequency").GetString());
        Assert.Equal(new[] { 3, 5 }, summary.GetProperty("daysOfWeek").EnumerateArray().Select(x => x.GetInt32()).ToArray());
        Assert.Equal("2030-05-01", summary.GetProperty("startDate").GetString());
        Assert.Equal("2030-05-10", summary.GetProperty("endDate").GetString());
        Assert.Equal(JsonValueKind.Null, singleDetail.GetProperty("series").ValueKind);
    }

    // SERIES-21
    [Fact]
    public async Task SERIES_21_Deactivate_from_middle_day_deactivates_that_day_and_after()
    {
        var user = await NewUserAsync();
        var seriesId = await CreateSeriesAsync(user,
            SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-05"), 5);

        var response = await SendAsync(HttpMethod.Post, $"/api/activities/series/{seriesId}/deactivate?from=2030-05-03", user.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, (await ReadDataAsync(response)).GetProperty("deactivatedCount").GetInt32());
        var activities = await Factory.WithDbAsync(db =>
            db.Activities.AsNoTracking().Where(a => a.SeriesId == seriesId).OrderBy(a => a.Start).ToListAsync());
        Assert.Equal(new[] { true, true, false, false, false }, activities.Select(a => a.IsActive).ToArray());
    }

    // SERIES-22
    [Fact]
    public async Task SERIES_22_Editing_one_occurrence_changes_only_it_and_keeps_SeriesId()
    {
        var user = await NewUserAsync();
        var seriesId = await CreateSeriesAsync(user,
            SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-03"), 3);
        var ids = await Factory.WithDbAsync(db =>
            db.Activities.AsNoTracking().Where(a => a.SeriesId == seriesId).OrderBy(a => a.Start).Select(a => a.Id).ToListAsync());
        var moved = new DateTimeOffset(2030, 5, 2, 22, 30, 0, Istanbul);

        var response = await SendAsync(HttpMethod.Put, $"/api/activities/{ids[1]}", user.Token, new Dictionary<string, object?>
        {
            ["CategoryId"] = 1, ["Title"] = "Değişti", ["Description"] = "x", ["Status"] = "Planned", ["Priority"] = "High",
            ["EnergyLevel"] = "Low", ["Start"] = moved, ["End"] = moved.AddMinutes(10), ["IsAllDay"] = false,
            ["IsFlexible"] = false, ["EstimatedBudget"] = 0m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var all = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().Where(a => a.SeriesId == seriesId).OrderBy(a => a.Id).ToListAsync());
        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "Krem sür", "Değişti", "Krem sür" }, all.Select(a => a.Title).ToArray());
        Assert.Equal(moved, all[1].Start);
        Assert.Equal(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), all[0].Start);
        Assert.Equal(new DateTimeOffset(2030, 5, 3, 21, 0, 0, Istanbul), all[2].Start);
    }

    // SERIES-23
    [Fact]
    public async Task SERIES_23_Deactivating_other_users_series_returns_404_and_changes_nothing()
    {
        var a = await NewUserAsync();
        var b = await NewUserAsync();
        var seriesId = await CreateSeriesAsync(b,
            SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-03"), 3);

        var response = await SendAsync(HttpMethod.Post, $"/api/activities/series/{seriesId}/deactivate?from=2030-01-01", a.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(3, await Factory.WithDbAsync(db => db.Activities.CountAsync(x => x.SeriesId == seriesId && x.IsActive)));
    }

    // SERIES-24 (AUTH-48 kapsamında da)
    [Theory]
    [InlineData("/api/activities/series")]
    [InlineData("/api/activities/series/1/deactivate?from=2030-01-01")]
    public async Task SERIES_24_Series_endpoints_without_token_return_401(string path)
    {
        object? body = path.EndsWith("series")
            ? SeriesBody(new DateTimeOffset(2030, 5, 1, 21, 0, 0, Istanbul), "Daily", "2030-05-03")
            : null;

        var response = await SendAsync(HttpMethod.Post, path, token: null, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
