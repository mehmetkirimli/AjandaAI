// Entegrasyon test sınıflarının ortak tabanıdır: HttpClient, JSON okuma ve kayıt oluşturma yardımcıları.
// Tüm sınıflar aynı fiziksel veritabanını paylaştığı için paralel çalışma kapalıdır (AssemblyInfo.cs).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    protected async Task<int> CreateUserAsync(string? email = null)
    {
        var response = await Client.PostAsJsonAsync("/api/users", new
        {
            Email = email ?? $"{Guid.NewGuid():N}@test.com",
            DisplayName = "Test Kullanıcı",
            TimeZoneId = "Europe/Istanbul"
        }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }

    protected static object ActivityBody(int userId, int categoryId = 1,
        DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        var s = start ?? DateTimeOffset.UtcNow.AddDays(7);
        return new
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = "Entegrasyon aktivitesi",
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

    protected async Task<int> CreateActivityAsync(int userId)
    {
        var response = await Client.PostAsJsonAsync("/api/activities", ActivityBody(userId), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }
}
