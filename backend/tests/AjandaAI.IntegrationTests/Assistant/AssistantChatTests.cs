// POST /api/assistant/chat ve asistan araçları (ADR 0025), gerçek HTTP + PostgreSQL, sahte model ve dış servisler.
// Senaryolar: ASSIST-02, 03, 10..13, 15..17, 20, 21, 24, 25, 30, 31, 40..43 (docs/asistan-test-senaryolari.md).
// Sahte model "şu aracı şu girdiyle çağır" der; araç sonucu modele dönünce sonucu metin olarak yanıtlar (CallThenEcho).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Assistant;
using AjandaAI.IntegrationTests.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Assistant;

public class AssistantChatTests : AssistantTestBase, IClassFixture<AssistantApiFactory>
{
    public AssistantChatTests(AssistantApiFactory factory) : base(factory) { }

    private static Dictionary<string, object?> Args(object values) =>
        JsonSerializer.SerializeToElement(values).EnumerateObject()
            .ToDictionary(p => p.Name, p => (object?)p.Value.Clone());

    private static object ActivityArgs(DateTimeOffset start, DateTimeOffset end, string title = "Yürüyüş") =>
        new { baslik = title, baslangic = start, bitis = end, kategoriId = 1 };

    // ASSIST-02
    [Fact]
    public async Task ASSIST_02_WithoutSession_Returns401()
    {
        var response = await ChatAsync(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ASSIST-03: kişisel erişim anahtarı yalnızca /mcp'de geçer.
    [Fact]
    public async Task ASSIST_03_PersonalAccessToken_Returns401()
    {
        var user = await NewUserAsync();
        var create = await SendAsync(user.Access, HttpMethod.Post, "/api/auth/tokens", new { Name = "Claude" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var pat = (await ReadDataAsync(create)).GetProperty("token").GetString()!;

        var response = await ChatAsync(pat);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ASSIST-10, ASSIST-42: öneri gövdesiyle mevcut POST /api/activities kaydı sohbet eden kullanıcıya oluşturur.
    [Fact]
    public async Task ASSIST_10_42_ActivityProposal_PostedToActivities_CreatesOwnedActivity()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(3);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.AktiviteOner,
            Args(ActivityArgs(At(day, 10), At(day, 11), "Asistan yürüyüşü")));

        var chat = await ChatAsync(user.Access, "Cumartesi 10'a yürüyüş ekle");
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);
        var proposal = (await ReadDataAsync(chat)).GetProperty("proposals")[0];
        Assert.Equal("activity", proposal.GetProperty("kind").GetString());

        var post = await SendAsync(user.Access, HttpMethod.Post, "/api/activities", proposal.GetProperty("activity"));

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var owner = await Factory.WithDbAsync(db =>
            db.Activities.Where(a => a.Title == "Asistan yürüyüşü").Select(a => a.UserId).SingleAsync());
        Assert.Equal(user.Id, owner);
        var list = await SendAsync(user.Access, HttpMethod.Get, "/api/activities");
        Assert.Contains("Asistan yürüyüşü", await list.Content.ReadAsStringAsync());
    }

    // ASSIST-11: yalnızca sohbet eden kullanıcının etkin aktiviteleri. ASSIST-13: girdideki başka userId yok sayılır.
    [Fact]
    public async Task ASSIST_11_13_WeekTool_ReturnsOnlyOwnActiveActivities_IgnoresUserIdInInput()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();
        var day = Today().AddDays(7);
        await SeedActivityAsync(user.Id, "Benim-etkin", At(day, 9));
        await SeedActivityAsync(user.Id, "Benim-pasif", At(day, 12), isActive: false);
        await SeedActivityAsync(other.Id, "Baskasinin", At(day, 15));
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.HaftayiListele,
            Args(new { tarih = day.ToString("yyyy-MM-dd"), userId = other.Id }));

        var reply = await ReplyAsync(await ChatAsync(user.Access, "Bu haftam nasıl?"));

        Assert.Contains("Benim-etkin", reply);
        Assert.DoesNotContain("Benim-pasif", reply);
        Assert.DoesNotContain("Baskasinin", reply);
    }

    // ASSIST-12, ASSIST-41: bitiş < başlangıç → Türkçe hata, öneri yok, kayıt yok, sohbet çökmez.
    [Fact]
    public async Task ASSIST_12_41_EndBeforeStart_TurkishError_NoProposal_NoRecord()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(2);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.AktiviteOner, Args(ActivityArgs(At(day, 11), At(day, 10))));

        var response = await ChatAsync(user.Access);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await ReadDataAsync(response);
        Assert.Contains("Bitiş zamanı başlangıçtan sonra olmalıdır.", data.GetProperty("reply").GetString());
        Assert.Equal(0, data.GetProperty("proposals").GetArrayLength());
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // ASSIST-12, ASSIST-41: 30 günden eski başlangıç → öneri üretilmez.
    [Fact]
    public async Task ASSIST_12_41_StartOlderThan30Days_TurkishError_NoProposal()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(-40);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.AktiviteOner, Args(ActivityArgs(At(day, 10), At(day, 11))));

        var data = await ReadDataAsync(await ChatAsync(user.Access));

        Assert.Contains(PastStartRule.Message, data.GetProperty("reply").GetString());
        Assert.Equal(0, data.GetProperty("proposals").GetArrayLength());
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // ASSIST-15: hava servisi hata verir → modele Türkçe araç hatası, sohbet 200 ile devam eder.
    [Fact]
    public async Task ASSIST_15_WeatherServiceFails_ToolErrorReturnedToModel_ChatContinues()
    {
        var user = await NewUserAsync();
        Assistant.External.Responder = r => r.RequestUri!.Host == FakeExternalHandler.Weather
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : FakeExternalHandler.Default(r);
        try
        {
            var day = Today().AddDays(1).ToString("yyyy-MM-dd");
            Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.HavaDurumu,
                Args(new { enlem = 41.0, boylam = 29.0, baslangicGunu = day, bitisGunu = day }));

            var response = await ChatAsync(user.Access, "Yarın hava nasıl?");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Hava durumu servisine şu an ulaşılamıyor", await ReplyAsync(response));
        }
        finally
        {
            Assistant.External.Responder = FakeExternalHandler.Default;
        }
    }

    // ASSIST-15 (başarılı yol): hava aracı sonucu modele döner ve araç adı yanıtta listelenir.
    [Fact]
    public async Task ASSIST_15_WeatherTool_ReturnsForecast_AndUsedToolsListed()
    {
        var user = await NewUserAsync();
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.HavaDurumu,
            Args(new { enlem = 41.0, boylam = 29.0, baslangicGunu = "2030-01-05", bitisGunu = "2030-01-05" }));

        var data = await ReadDataAsync(await ChatAsync(user.Access));

        Assert.Contains("Açık", data.GetProperty("reply").GetString());
        Assert.Contains(AssistantToolNames.HavaDurumu, UsedTools(data));
    }

    // ASSIST-16: aynı sorgu ikinci kez önbellekten; dış servise saniyede en fazla 1 istek, uygulama User-Agent'ı.
    [Fact]
    public async Task ASSIST_16_PlaceSearch_CachedSecondTime_ThrottledToOnePerSecond_WithUserAgent()
    {
        var user = await NewUserAsync();
        var query = "Belgrad Ormanı " + Guid.NewGuid().ToString("N")[..6];
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.YerAra, Args(new { sorgu = query }));

        Assert.Contains("41.18", await ReplyAsync(await ChatAsync(user.Access)));
        Assert.Contains("41.18", await ReplyAsync(await ChatAsync(user.Access)));
        var sameQuery = Assistant.External.For(FakeExternalHandler.Places).Where(r => r.Url.Contains(Uri.EscapeDataString(query))).ToList();
        Assert.Single(sameQuery);
        Assert.StartsWith("AjandaAI/1.0", sameQuery[0].UserAgent);

        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.YerAra, Args(new { sorgu = query + " ikinci" }));
        await ChatAsync(user.Access);
        var all = Assistant.External.For(FakeExternalHandler.Places).OrderBy(r => r.At).ToList();
        for (var i = 1; i < all.Count; i++)
            Assert.True(all[i].At - all[i - 1].At >= TimeSpan.FromMilliseconds(950), "Nominatim'e saniyede birden fazla istek gitti.");
    }

    // ASSIST-17: aynı yıl ikinci kez önbellekten; servis hata verirse modele Türkçe hata, sohbet devam eder.
    [Fact]
    public async Task ASSIST_17_Holidays_CachedPerYear_ServiceErrorReturnsTurkishToolError()
    {
        var user = await NewUserAsync();
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.TatilleriListele, Args(new { yil = 2031 }));

        Assert.Contains("Ulusal Egemenlik", await ReplyAsync(await ChatAsync(user.Access)));
        Assert.Contains("Ulusal Egemenlik", await ReplyAsync(await ChatAsync(user.Access)));
        Assert.Single(Assistant.External.For(FakeExternalHandler.Holidays), r => r.Url.Contains("/2031/TR"));

        Assistant.External.Responder = r => r.RequestUri!.Host == FakeExternalHandler.Holidays
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : FakeExternalHandler.Default(r);
        try
        {
            Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.TatilleriListele, Args(new { yil = 2032 }));
            var response = await ChatAsync(user.Access);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Tatil servisine şu an ulaşılamıyor", await ReplyAsync(response));
        }
        finally
        {
            Assistant.External.Responder = FakeExternalHandler.Default;
        }
    }

    // ASSIST-20
    [Fact]
    public async Task ASSIST_20_MessageLongerThan2000_Returns400()
    {
        var user = await NewUserAsync();

        var response = await ChatAsync(user.Access, new string('a', 2001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ASSIST-21
    [Fact]
    public async Task ASSIST_21_HistoryLongerThan20_Returns400()
    {
        var user = await NewUserAsync();
        var history = Enumerable.Range(0, 21).Select(i => new { role = i % 2 == 0 ? "user" : "assistant", text = "x" }).ToList();

        var response = await ChatAsync(user.Access, "Merhaba", history);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ASSIST-21 (sınırda): 20 mesajlık geçmiş kabul edilir ve modele iletilir.
    [Fact]
    public async Task ASSIST_21_HistoryOf20_IsAcceptedAndPassedToModel()
    {
        var user = await NewUserAsync();
        var history = Enumerable.Range(0, 20).Select(i => new { role = i % 2 == 0 ? "user" : "assistant", text = "g" + i }).ToList();
        Model.Script = _ => ScriptedChatClient.Text("Tamam.");

        var response = await ChatAsync(user.Access, "Son mesaj", history);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Model.Requests.Last();
        Assert.Equal(22, request.Messages.Count); // sistem + 20 geçmiş + yeni mesaj
        Assert.Equal(ChatRole.Assistant, request.Messages[2].Role);
    }

    // ASSIST-24: model her turda yeniden araç çağırır → 8 çağrıdan sonra döngü durur, Türkçe açıklama.
    [Fact]
    public async Task ASSIST_24_MoreThan8ToolCalls_LoopStops_TurkishExplanation()
    {
        var user = await NewUserAsync();
        Model.Script = _ => ScriptedChatClient.Call(AssistantToolNames.KategorileriListele);

        var response = await ChatAsync(user.Access);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AssistantService.ToolLimitMessage, await ReplyAsync(response));
        var toolLogs = Assistant.Logs.Events.Count(e =>
            e.MessageTemplate.Text.StartsWith("Asistan aracı çalıştı") &&
            e.Properties.TryGetValue("UserId", out var id) && id.ToString() == user.Id.ToString());
        Assert.Equal(AssistantService.MaxToolCallsPerRequest, toolLogs);
    }

    // ASSIST-25: sağlayıcı 429/5xx → 503 "yoğun"; uygulamanın diğer uçları etkilenmez.
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ASSIST_25_ProviderQuotaOrServerError_Returns503Busy(HttpStatusCode status)
    {
        var user = await NewUserAsync();
        Model.Script = _ => throw new HttpRequestException("sağlayıcı hatası", null, status);

        var response = await ChatAsync(user.Access);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(AssistantService.BusyMessage, (await ReadRootAsync(response)).GetProperty("message").GetString());
        var categories = await SendAsync(user.Access, HttpMethod.Get, "/api/categories");
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    // ASSIST-30: mesaj ve cevap metni loglarda yok; araç adı, süre ve token sayısı var.
    [Fact]
    public async Task ASSIST_30_Logs_ContainToolNameDurationTokens_NotMessageOrReplyText()
    {
        var user = await NewUserAsync();
        var secretMessage = "gizli-mesaj-" + Guid.NewGuid().ToString("N");
        var secretReply = "gizli-cevap-" + Guid.NewGuid().ToString("N");
        Model.Script = messages => ScriptedChatClient.ToolResults(messages).Count == 0
            ? ScriptedChatClient.Call(AssistantToolNames.KategorileriListele)
            : ScriptedChatClient.Text(secretReply);

        var response = await ChatAsync(user.Access, secretMessage);
        Assert.Equal(secretReply, await ReplyAsync(response));

        var all = Assistant.Logs.Events.Select(FullText).ToList();
        Assert.DoesNotContain(all, t => t.Contains(secretMessage) || t.Contains(secretReply));

        var mine = Assistant.Logs.Events.Where(e =>
            e.Properties.TryGetValue("UserId", out var id) && id.ToString() == user.Id.ToString()).ToList();
        var tool = Assert.Single(mine, e => e.MessageTemplate.Text.StartsWith("Asistan aracı çalıştı"));
        Assert.Equal($"\"{AssistantToolNames.KategorileriListele}\"", tool.Properties["Tool"].ToString());
        Assert.True(tool.Properties.ContainsKey("ElapsedMs"));
        var summary = Assert.Single(mine, e => e.MessageTemplate.Text.StartsWith("Asistan yanıtı"));
        Assert.Equal("18", summary.Properties["TotalTokens"].ToString());
        Assert.True(summary.Properties.ContainsKey("ElapsedMs"));

        static string FullText(LogEvent e) =>
            e.RenderMessage() + " " + string.Join(" ", e.Properties.Values.Select(v => v.ToString())) + " " + e.Exception;
    }

    // ASSIST-31: sistem talimatı kullanıcının saat dilimini ve yerel "bugün"ü içerir.
    [Fact]
    public async Task ASSIST_31_SystemPrompt_ContainsUserTimeZoneAndLocalToday()
    {
        var user = await NewUserAsync();
        Model.Script = _ => ScriptedChatClient.Text("Tamam.");

        await ChatAsync(user.Access);

        var system = Model.Requests.Last().Messages[0];
        Assert.Equal(ChatRole.System, system.Role);
        Assert.Contains("Europe/Istanbul", system.Text);
        Assert.Contains(Today().ToString("yyyy-MM-dd"), system.Text);
    }

    // ASSIST-40: seri_oner veritabanına yazmaz, yapılandırılmış öneri döner. ASSIST-42: öneri /series ucundan kaydedilir.
    [Fact]
    public async Task ASSIST_40_42_SeriesProposal_WritesNothing_ThenSeriesEndpointCreates()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(1);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.SeriOner, Args(new
        {
            baslik = "Kitap", ilkBaslangic = At(day, 23), ilkBitis = At(day, 23, 10), kategoriId = 1,
            siklik = "Daily", sonGun = day.AddDays(6).ToString("yyyy-MM-dd")
        }));

        var data = await ReadDataAsync(await ChatAsync(user.Access, "Bir hafta her gece kitap"));

        var proposal = Assert.Single(data.GetProperty("proposals").EnumerateArray());
        Assert.Equal("series", proposal.GetProperty("kind").GetString());
        Assert.Equal("Daily", proposal.GetProperty("series").GetProperty("recurrence").GetProperty("frequency").GetString());
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
        Assert.Equal(0, await Factory.WithDbAsync(db => db.ActivitySeries.CountAsync(s => s.UserId == user.Id)));

        var post = await SendAsync(user.Access, HttpMethod.Post, "/api/activities/series", proposal.GetProperty("series"));

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.Equal(7, await CountActivitiesAsync(user.Id));
    }

    // ASSIST-40, ASSIST-43: aktivite önerisi yazmaz; kullanıcı "Vazgeç" derse (hiç POST yok) kayıt oluşmaz.
    [Fact]
    public async Task ASSIST_40_43_ActivityProposal_WithoutConfirmation_CreatesNothing()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(4);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.AktiviteOner, Args(ActivityArgs(At(day, 18), At(day, 19))));

        var data = await ReadDataAsync(await ChatAsync(user.Access));

        Assert.Equal(1, data.GetProperty("proposals").GetArrayLength());
        Assert.Contains(AssistantToolNames.AktiviteOner, UsedTools(data));
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }

    // ASSIST-42: öneri gövdesi web formuyla aynı doğrulamadan geçer (değiştirilip geçersiz yapılırsa 400).
    [Fact]
    public async Task ASSIST_42_EditedProposalInvalid_ActivitiesEndpointRejects()
    {
        var user = await NewUserAsync();
        var day = Today().AddDays(5);
        Model.Script = ScriptedChatClient.CallThenEcho(AssistantToolNames.AktiviteOner, Args(ActivityArgs(At(day, 8), At(day, 9))));
        var activity = (await ReadDataAsync(await ChatAsync(user.Access))).GetProperty("proposals")[0].GetProperty("activity");

        var edited = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(activity.GetRawText())!;
        edited["end"] = JsonSerializer.SerializeToElement(At(day, 7));
        var post = await SendAsync(user.Access, HttpMethod.Post, "/api/activities", edited);

        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        Assert.Equal(0, await CountActivitiesAsync(user.Id));
    }
}
