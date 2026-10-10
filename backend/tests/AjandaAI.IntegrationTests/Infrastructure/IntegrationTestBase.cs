// Entegrasyon test sınıflarının ortak tabanıdır: HttpClient, JSON okuma ve kayıt oluşturma yardımcıları.
// Tüm sınıflar aynı fiziksel veritabanını paylaştığı için paralel çalışma kapalıdır (AssemblyInfo.cs).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<AjandaApiFactory>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected AjandaApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected IntegrationTestBase(AjandaApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    // /api/activities artık token ister (P4); Reminder testleri kimliksiz çalıştığı için aktivite DB'ye doğrudan eklenir.
    protected async Task<int> CreateActivityAsync(int userId)
    {
        var now = DateTimeOffset.UtcNow;
        var start = now.AddDays(7);
        return await Factory.WithDbAsync(async db =>
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

    protected Task DeactivateActivityAsync(int activityId) =>
        Factory.WithDbAsync(db => db.Activities.Where(a => a.Id == activityId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false)));
}
