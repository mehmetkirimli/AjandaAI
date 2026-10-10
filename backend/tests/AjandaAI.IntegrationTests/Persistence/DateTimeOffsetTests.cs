// timestamptz yalnızca UTC offset'li DateTimeOffset kabul eder (Npgsql). İstemci "+03:00" gönderdiğinde
// 500 dönmemeli: değer UTC'ye çevrilip saklanır, aynı ANI temsil eder (ADR 0004, ADR 0021 F1 bulgusu).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Persistence;

public class DateTimeOffsetTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public DateTimeOffsetTests(AuthApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Activity_WithNonUtcOffset_IsSavedAsSameInstantInUtc()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        var categoryId = await Factory.WithDbAsync(db => db.Categories.Where(c => c.IsActive).Select(c => c.Id).FirstAsync());
        var start = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.FromHours(3));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/activities")
        {
            Content = JsonContent.Create(new
            {
                CategoryId = categoryId, Title = "Offset testi", Description = "", Status = "Planned",
                Priority = "Medium", EnergyLevel = "Medium",
                Start = "2026-10-12T10:00:00+03:00", End = "2026-10-12T11:30:00+03:00",
                IsAllDay = false, Location = (string?)null, IsFlexible = false, EstimatedBudget = 0
            }, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadDataAsync(response)).GetProperty("id").GetInt32();
        var row = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == id));
        Assert.Equal(start, row.Start);                       // aynı an
        Assert.Equal(TimeSpan.Zero, row.Start.Offset);         // UTC saklanır
        Assert.Equal(start.AddMinutes(90).UtcDateTime, row.End.UtcDateTime);
    }
}
