// /mcp ucu ve Claude araçları (ADR 0024 §1, §2, §2b), gerçek HTTP + PostgreSQL.
// Senaryolar: MCP-10, 11, 16, 17, 19, 20..33 ve PAST-06 (docs/mcp-test-senaryolari.md).
// İstemci kütüphanesi kullanılmaz: durumsuz Streamable HTTP'ye ham JSON-RPC atılır, yanıt JSON veya SSE gelebilir.
// Test kullanıcıları Europe/Istanbul saat dilimindedir (AuthTestBase); tarihler ileriye, "bugün + N gün"e kurulur.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Mcp;

public class McpTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public McpTests(AuthApiFactory factory) : base(factory) { }

    private sealed record TestUser(int Id, string Access, string Pat, int PatId);

    private sealed record ToolResult(bool IsError, string Text)
    {
        public JsonElement Json => JsonDocument.Parse(Text).RootElement;
    }

    private async Task<TestUser> NewUserAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/tokens")
        {
            Content = JsonContent.Create(new { Name = "Claude" }, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        return new TestUser(id, access, data.GetProperty("token").GetString()!, data.GetProperty("id").GetInt32());
    }

    private Task<HttpResponseMessage> RpcAsync(string? bearer, string method, object? parameters = null)
    {
        var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = parameters ?? new { } });
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return Client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadRpcAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var json = text.TrimStart().StartsWith('{')
            ? text
            : string.Join("", text.Split('\n').Where(l => l.StartsWith("data:")).Select(l => l["data:".Length..].Trim()));
        return JsonDocument.Parse(json).RootElement;
    }

    private async Task<ToolResult> ToolAsync(string pat, string name, object arguments)
    {
        var root = await ReadRpcAsync(await RpcAsync(pat, "tools/call", new { name, arguments }));
        var result = root.GetProperty("result");
        var isError = result.TryGetProperty("isError", out var e) && e.GetBoolean();
        var text = string.Join("", result.GetProperty("content").EnumerateArray().Select(c => c.GetProperty("text").GetString()));
        return new ToolResult(isError, text);
    }

    private static DateOnly FutureDay(int days) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Istanbul).DateTime).AddDays(days);

    private static DateTimeOffset At(DateOnly day, int hour, int minute = 0) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), Istanbul.GetUtcOffset(day.ToDateTime(new TimeOnly(hour, minute))));

    private static object AddArgs(DateTimeOffset start, DateTimeOffset end, int categoryId = 1, string title = "Sinema") =>
        new { baslik = title, baslangic = start, bitis = end, kategoriId = categoryId };

    private Task<int> SeedActivityAsync(int userId, DateTimeOffset start, DateTimeOffset end, bool isActive = true) =>
        Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var activity = new Activity
            {
                UserId = userId, CategoryId = 1, Title = "Tohum", Description = "", Status = ActivityStatus.Planned,
                Priority = Priority.Medium, EnergyLevel = EnergyLevel.Low, Start = start, End = end,
                IsActive = isActive, CreatedAt = now, UpdatedAt = now
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return activity.Id;
        });

    private Task<int> CountActivitiesAsync(int userId) =>
        Factory.WithDbAsync(db => db.Activities.CountAsync(a => a.UserId == userId));

    // MCP-10: geçerli anahtar → araç çalışır, LastUsedAt dolar.
    [Fact]
    public async Task MCP10_ValidToken_ToolRuns_AndLastUsedAtIsSet()
    {
        var user = await NewUserAsync();

        var result = await ToolAsync(user.Pat, "kategorileri_listele", new { });

        Assert.False(result.IsError);
        var lastUsed = await Factory.WithDbAsync(db =>
            db.PersonalAccessTokens.Where(t => t.Id == user.PatId).Select(t => t.LastUsedAt).SingleAsync());
        Assert.NotNull(lastUsed);
    }

    // MCP-11: anahtarsız istek → 401.
    [Fact]
    public async Task MCP11_NoToken_Returns401()
    {
        var response = await RpcAsync(null, "tools/list");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // MCP-16: anahtar normal API'de geçmez.
    [Fact]
    public async Task MCP16_TokenOnApiEndpoint_Returns401()
    {
        var user = await NewUserAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/activities");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Pat);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // MCP-17: JWT /mcp'de geçmez.
    [Fact]
    public async Task MCP17_JwtOnMcp_Returns401()
    {
        var user = await NewUserAsync();

        var response = await RpcAsync(user.Access, "tools/list");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // MCP-19: anahtarlı isteklerin loglarında anahtar görünmez.
    [Fact]
    public async Task MCP19_Logs_DoNotContainToken()
    {
        var user = await NewUserAsync();
        await ToolAsync(user.Pat, "kategorileri_listele", new { });
        await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(At(FutureDay(5), 10), At(FutureDay(5), 9)));
        await RpcAsync(user.Pat + "bozuk", "tools/list");

        static string FullText(LogEvent e) =>
            e.RenderMessage() + string.Join("", e.Properties.Select(p => p.Value.ToString())) + e.Exception;
        Assert.DoesNotContain(Factory.Logs.Events, e => FullText(e).Contains(user.Pat[4..]));
    }

    // MCP-20: aktivite_ekle → anahtar sahibine yazılır, web listesinde görünür.
    [Fact]
    public async Task MCP20_AddActivity_OwnedByTokenUser_VisibleOnWeb()
    {
        var user = await NewUserAsync();
        var day = FutureDay(7);

        var result = await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(At(day, 19), At(day, 21)));

        Assert.False(result.IsError, result.Text);
        var id = result.Json.GetProperty("id").GetInt32();
        var owner = await Factory.WithDbAsync(db => db.Activities.Where(a => a.Id == id).Select(a => a.UserId).SingleAsync());
        Assert.Equal(user.Id, owner);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/activities?pageSize=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Access);
        var items = (await ReadDataAsync(await Client.SendAsync(request))).GetProperty("items");
        Assert.Contains(items.EnumerateArray(), a => a.GetProperty("id").GetInt32() == id);
    }

    // MCP-21: bitiş < başlangıç → araç hatası (Türkçe), kayıt yok.
    [Fact]
    public async Task MCP21_EndBeforeStart_ToolError_NoRecord()
    {
        var user = await NewUserAsync();
        var day = FutureDay(7);

        var result = await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(At(day, 19), At(day, 18)));

        Assert.True(result.IsError);
        Assert.Contains("Doğrulama hatası", result.Text);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // MCP-22: olmayan veya pasif kategori → araç hatası, kayıt yok.
    [Fact]
    public async Task MCP22_MissingOrInactiveCategory_ToolError_NoRecord()
    {
        var user = await NewUserAsync();
        var inactiveId = await Factory.WithDbAsync(async db =>
        {
            var category = new Category { Name = $"Pasif {Guid.NewGuid():N}", IsActive = false, SortOrder = 999, CreatedAt = DateTimeOffset.UtcNow };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        });
        var day = FutureDay(7);

        var missing = await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(At(day, 19), At(day, 20), categoryId: 987654));
        var inactive = await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(At(day, 19), At(day, 20), categoryId: inactiveId));

        Assert.True(missing.IsError);
        Assert.True(inactive.IsError);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // MCP-23: girdiye başka bir userId eklenirse yok sayılır.
    [Fact]
    public async Task MCP23_ForeignUserIdInArguments_IsIgnored()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();
        var day = FutureDay(7);

        var result = await ToolAsync(user.Pat, "aktivite_ekle", new
        {
            baslik = "Sızma", baslangic = At(day, 10), bitis = At(day, 11), kategoriId = 1, userId = other.Id
        });

        Assert.False(result.IsError, result.Text);
        Assert.Equal(1, await CountActivitiesAsync(user.Id));
        Assert.Equal(0, await CountActivitiesAsync(other.Id));
    }

    // MCP-24: haftayi_listele yalnızca anahtar sahibinin etkin aktiviteleri.
    [Fact]
    public async Task MCP24_ListWeek_OnlyOwnActive()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();
        var day = FutureDay(14);
        var own = await SeedActivityAsync(user.Id, At(day, 10), At(day, 11));
        var inactive = await SeedActivityAsync(user.Id, At(day, 12), At(day, 13), isActive: false);
        var foreign = await SeedActivityAsync(other.Id, At(day, 14), At(day, 15));

        var result = await ToolAsync(user.Pat, "haftayi_listele", new { tarih = day });

        Assert.False(result.IsError, result.Text);
        var ids = result.Json.GetProperty("activities").EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(own, ids);
        Assert.DoesNotContain(inactive, ids);
        Assert.DoesNotContain(foreign, ids);
    }

    // MCP-25: hafta sınırı kullanıcının saat diliminde (Pazar 23:30 dahil, Pazartesi 00:30 hariç).
    [Fact]
    public async Task MCP25_ListWeek_BoundaryInUserTimeZone()
    {
        var user = await NewUserAsync();
        var day = FutureDay(21);
        var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        var sundayLate = await SeedActivityAsync(user.Id, At(monday.AddDays(6), 23, 30), At(monday.AddDays(6), 23, 50));
        var nextMonday = await SeedActivityAsync(user.Id, At(monday.AddDays(7), 0, 30), At(monday.AddDays(7), 1));

        var result = await ToolAsync(user.Pat, "haftayi_listele", new { tarih = monday.AddDays(2) });

        Assert.False(result.IsError, result.Text);
        Assert.Equal(monday.ToString("yyyy-MM-dd"), result.Json.GetProperty("weekStart").GetString());
        var ids = result.Json.GetProperty("activities").EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(sundayLate, ids);
        Assert.DoesNotContain(nextMonday, ids);
    }

    private static List<(string Start, string End)> Slots(ToolResult result) =>
        result.Json.EnumerateArray()
            .Select(s => (s.GetProperty("start").GetDateTimeOffset().ToString("HH:mm"), s.GetProperty("end").GetDateTimeOffset().ToString("HH:mm")))
            .ToList();

    // MCP-26: pencere içindeki, en az süreyi karşılayan boşluklar.
    [Fact]
    public async Task MCP26_FreeTime_RespectsWindowAndMinimum()
    {
        var user = await NewUserAsync();
        var day = FutureDay(9);
        await SeedActivityAsync(user.Id, At(day, 10), At(day, 10, 45));
        var args = new { baslangicGunu = day, bitisGunu = day, gunBaslangici = "09:00", gunBitisi = "12:00", enAzDakika = 30 };

        var all = await ToolAsync(user.Pat, "bos_zaman_bul", args);
        var longer = await ToolAsync(user.Pat, "bos_zaman_bul", args with { enAzDakika = 70 });

        Assert.False(all.IsError, all.Text);
        Assert.Equal(new[] { ("09:00", "10:00"), ("10:45", "12:00") }, Slots(all));
        Assert.Equal(new[] { ("10:45", "12:00") }, Slots(longer));
    }

    // MCP-27: gece yarısını aşan aktivite iki günü de doğru böler.
    [Fact]
    public async Task MCP27_FreeTime_OvernightActivity()
    {
        var user = await NewUserAsync();
        var day = FutureDay(11);
        await SeedActivityAsync(user.Id, At(day, 23), At(day.AddDays(1), 1));

        var result = await ToolAsync(user.Pat, "bos_zaman_bul",
            new { baslangicGunu = day, bitisGunu = day.AddDays(1), gunBaslangici = "00:00", gunBitisi = "23:59", enAzDakika = 30 });

        Assert.False(result.IsError, result.Text);
        Assert.Equal(new[] { ("00:00", "23:00"), ("01:00", "23:59") }, Slots(result));
    }

    // MCP-28: 31 günden uzun aralık → araç hatası.
    [Fact]
    public async Task MCP28_FreeTime_RangeOver31Days_ToolError()
    {
        var user = await NewUserAsync();
        var day = FutureDay(1);

        var result = await ToolAsync(user.Pat, "bos_zaman_bul", new { baslangicGunu = day, bitisGunu = day.AddDays(31) });

        Assert.True(result.IsError);
        Assert.Contains("31", result.Text);
    }

    // MCP-29: yalnızca etkin kategoriler, yönetim sırasıyla.
    [Fact]
    public async Task MCP29_Categories_OnlyActive_InSortOrder()
    {
        var user = await NewUserAsync();
        var inactiveName = $"Pasif {Guid.NewGuid():N}";
        await Factory.WithDbAsync(async db =>
        {
            db.Categories.Add(new Category { Name = inactiveName, IsActive = false, SortOrder = 0, CreatedAt = DateTimeOffset.UtcNow });
            return await db.SaveChangesAsync();
        });

        var result = await ToolAsync(user.Pat, "kategorileri_listele", new { });

        Assert.False(result.IsError, result.Text);
        var items = result.Json.EnumerateArray().ToList();
        Assert.All(items, c => Assert.True(c.GetProperty("isActive").GetBoolean()));
        Assert.DoesNotContain(items, c => c.GetProperty("name").GetString() == inactiveName);
        var orders = items.Select(c => c.GetProperty("sortOrder").GetInt32()).ToList();
        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    // MCP-30: tam 5 araç, silme/güncelleme yok.
    [Fact]
    public async Task MCP30_ToolList_ExactlyFiveTools()
    {
        var user = await NewUserAsync();

        var root = await ReadRpcAsync(await RpcAsync(user.Pat, "tools/list"));

        var names = root.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(t => t.GetProperty("name").GetString()).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "aktivite_ekle", "bos_zaman_bul", "haftayi_listele", "kategorileri_listele", "seri_ekle" }, names);
    }

    // MCP-31: seri_ekle "her gün 23:00, 1 yıl" → tek çağrıda 365 oluşum, hepsi aynı seride ve anahtar sahibinde.
    [Fact]
    public async Task MCP31_AddSeries_365Occurrences_InOneCall()
    {
        var user = await NewUserAsync();
        var day = FutureDay(1);

        var result = await ToolAsync(user.Pat, "seri_ekle", new
        {
            baslik = "Kitap oku", ilkBaslangic = At(day, 23), ilkBitis = At(day, 23, 10), kategoriId = 1,
            siklik = "Daily", sonGun = day.AddDays(364)
        });

        Assert.False(result.IsError, result.Text);
        Assert.Equal(365, result.Json.GetProperty("createdCount").GetInt32());
        var seriesId = result.Json.GetProperty("seriesId").GetInt32();
        var counts = await Factory.WithDbAsync(async db => (
            Series: await db.Activities.CountAsync(a => a.SeriesId == seriesId),
            Owned: await db.Activities.CountAsync(a => a.SeriesId == seriesId && a.UserId == user.Id)));
        Assert.Equal(365, counts.Series);
        Assert.Equal(365, counts.Owned);
    }

    // MCP-32: 366'dan fazla oluşum → araç hatası, hiçbir oluşum yazılmaz.
    [Fact]
    public async Task MCP32_AddSeries_TooMany_ToolError_NothingWritten()
    {
        var user = await NewUserAsync();
        var day = FutureDay(1);

        var result = await ToolAsync(user.Pat, "seri_ekle", new
        {
            baslik = "Çok uzun", ilkBaslangic = At(day, 7), ilkBitis = At(day, 7, 30), kategoriId = 1,
            siklik = "Daily", sonGun = day.AddDays(400)
        });

        Assert.True(result.IsError);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // MCP-33: bir anahtarla 1 dakikada 31. çağrı → 429; başka anahtar etkilenmez.
    [Fact]
    public async Task MCP33_RateLimit_PerToken()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();

        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.OK, (await RpcAsync(user.Pat, "tools/list")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await RpcAsync(user.Pat, "tools/list")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RpcAsync(other.Pat, "tools/list")).StatusCode);
    }

    // PAST-06: aktivite_ekle 31 gün önceye → araç hatası, web formuyla aynı mesaj.
    [Fact]
    public async Task PAST06_AddActivity_31DaysAgo_ToolError()
    {
        var user = await NewUserAsync();
        var start = DateTimeOffset.UtcNow.AddDays(-31);

        var result = await ToolAsync(user.Pat, "aktivite_ekle", AddArgs(start, start.AddHours(1)));

        Assert.True(result.IsError);
        Assert.Contains("30 gün", result.Text);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }
}
