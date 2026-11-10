// Asistan entegrasyon testlerinin ortak yardımcıları: kullanıcı + oturum, sohbet isteği, tarih yardımcıları.
// Test kullanıcıları Europe/Istanbul saat dilimindedir (AuthTestBase); tarihler "bugün + N gün"e kurulur.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Assistant;

public abstract class AssistantTestBase : AuthTestBase
{
    protected static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    protected AssistantTestBase(AssistantApiFactory factory) : base(factory)
    {
        Assistant = factory;
    }

    protected AssistantApiFactory Assistant { get; }

    protected ScriptedChatClient Model => Assistant.Model;

    protected sealed record TestUser(int Id, string Access);

    protected async Task<TestUser> NewUserAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        return new TestUser(id, access);
    }

    protected Task<HttpResponseMessage> ChatAsync(string? bearer, string message = "Merhaba", object? history = null) =>
        SendAsync(bearer, HttpMethod.Post, "/api/assistant/chat", new { message, history });

    protected Task<HttpResponseMessage> SendAsync(string? bearer, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return Client.SendAsync(request);
    }

    protected static async Task<string> ReplyAsync(HttpResponseMessage response) =>
        (await ReadDataAsync(response)).GetProperty("reply").GetString()!;

    protected static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Istanbul).DateTime);

    protected static DateTimeOffset At(DateOnly day, int hour, int minute = 0)
    {
        var local = day.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(local, Istanbul.GetUtcOffset(local));
    }

    protected Task<int> SeedActivityAsync(int userId, string title, DateTimeOffset start, bool isActive = true) =>
        Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var activity = new Activity
            {
                UserId = userId, CategoryId = 1, Title = title, Description = "", Status = ActivityStatus.Planned,
                Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low, Start = start, End = start.AddHours(1),
                IsActive = isActive, CreatedAt = now, UpdatedAt = now
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return activity.Id;
        });

    protected Task<int> CountActivitiesAsync(int userId) =>
        Factory.WithDbAsync(db => db.Activities.CountAsync(a => a.UserId == userId));

    protected static List<string> UsedTools(JsonElement data) =>
        data.GetProperty("usedTools").EnumerateArray().Select(e => e.GetString()!).ToList();
}
