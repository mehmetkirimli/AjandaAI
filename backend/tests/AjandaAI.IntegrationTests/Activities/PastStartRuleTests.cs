// Geçmiş tarih kuralını gerçek HTTP + gerçek PostgreSQL üzerinden doğrular (ADR 0024 §2b).
// Senaryolar: PAST-01, 02, 03, 04, 05 (docs/mcp-test-senaryolari.md). PAST-06 MCP aracıdır, burada değil.
// Tarihler gerçek "şimdi"ye göredir; 29/31/40 gün sınırdan uzak olduğu için saat dilimi farkı sonucu değiştirmez.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.Application.Activities;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Activities;

public class PastStartRuleTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public PastStartRuleTests(AuthApiFactory factory) : base(factory) { }

    private sealed record TestUser(int Id, string Token);

    private async Task<TestUser> NewUserAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        return new TestUser(id, access);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object body)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(request);
    }

    // Saniyeye yuvarlanır: PostgreSQL mikrosaniye tutar, gidiş-dönüşte başlangıç "değişmiş" görünmesin.
    private static DateTimeOffset DaysAgo(int days)
    {
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond)).AddDays(-days);
    }

    private static object Body(DateTimeOffset start, string status = "Planned", int? rating = null, string description = "Açıklama") => new
    {
        CategoryId = 1, Title = "Geçmiş aktivite", Description = description, Status = status,
        Priority = "Medium", EnergyLevel = "Low", Start = start, End = start.AddHours(1),
        IsAllDay = false, Location = (string?)null, IsFlexible = false, EstimatedBudget = 0m,
        Rating = rating, WouldRepeat = (bool?)null
    };

    private async Task<List<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadRootAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).ToList();

    private Task<int> SeedOldActivityAsync(int userId, DateTimeOffset start) =>
        Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var activity = new Activity
            {
                UserId = userId, CategoryId = 1, Title = "Eski aktivite", Description = "",
                Status = ActivityStatus.Completed, Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low,
                Start = start, End = start.AddHours(1), CreatedAt = now, UpdatedAt = now
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return activity.Id;
        });

    // PAST-01
    [Fact]
    public async Task PAST_01_Create_29_days_ago_returns_201()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, Body(DaysAgo(29)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // PAST-02
    [Fact]
    public async Task PAST_02_Create_31_days_ago_returns_400_with_turkish_message()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/activities", user.Token, Body(DaysAgo(31)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Başlangıç en fazla 30 gün öncesi olabilir.", await ErrorsAsync(response));
        Assert.Equal(0, await Factory.WithDbAsync(db => db.Activities.CountAsync(a => a.UserId == user.Id)));
    }

    // PAST-03: başlangıç değişmediği için kural uygulanmaz.
    [Fact]
    public async Task PAST_03_Update_rating_and_note_of_40_day_old_activity_returns_200()
    {
        var user = await NewUserAsync();
        var start = DaysAgo(40);
        var id = await SeedOldActivityAsync(user.Id, start);

        var response = await SendAsync(HttpMethod.Put, $"/api/activities/{id}", user.Token,
            Body(start, status: "Completed", rating: 9, description: "Sonradan eklenen not"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal(9, saved.Rating);
        Assert.Equal("Sonradan eklenen not", saved.Description);
    }

    // PAST-04: form güncellemesi ve takvim/pano sürükle-bırakı aynı PUT ucunu kullanır.
    [Fact]
    public async Task PAST_04_Update_moving_start_to_31_days_ago_returns_400()
    {
        var user = await NewUserAsync();
        var original = DateTimeOffset.UtcNow.AddDays(3);
        var id = await SeedOldActivityAsync(user.Id, original);

        var response = await SendAsync(HttpMethod.Put, $"/api/activities/{id}", user.Token, Body(DaysAgo(31)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(PastStartRule.Message, await ErrorsAsync(response));
        var saved = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal(original.ToUnixTimeSeconds(), saved.Start.ToUnixTimeSeconds());
    }

    // PAST-05
    [Fact]
    public async Task PAST_05_Series_first_occurrence_31_days_ago_returns_400_and_writes_nothing()
    {
        var user = await NewUserAsync();
        var start = DaysAgo(31);
        var body = new Dictionary<string, object?>
        {
            ["CategoryId"] = 1, ["Title"] = "Krem sür", ["Description"] = "", ["Status"] = "Planned",
            ["Priority"] = "Medium", ["EnergyLevel"] = "Low", ["Start"] = start, ["End"] = start.AddMinutes(10),
            ["IsAllDay"] = false, ["IsFlexible"] = false, ["EstimatedBudget"] = 0m,
            ["Recurrence"] = new Dictionary<string, object?>
            {
                ["Frequency"] = "Daily", ["DaysOfWeek"] = Array.Empty<int>(),
                ["Until"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToString("yyyy-MM-dd")
            },
            ["ReminderMinutesBefore"] = null
        };

        var response = await SendAsync(HttpMethod.Post, "/api/activities/series", user.Token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(PastStartRule.Message, await ErrorsAsync(response));
        Assert.Equal(0, await Factory.WithDbAsync(db => db.Activities.CountAsync(a => a.UserId == user.Id)));
        Assert.Equal(0, await Factory.WithDbAsync(db => db.ActivitySeries.CountAsync(s => s.UserId == user.Id)));
    }
}
