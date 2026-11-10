// Asistanın yapılandırma, araç aç/kapa ve istek sınırı senaryoları (ADR 0025): her sınıf kendi factory'sini
// (kendi ayarını ve rate limit sayacını) alır. Senaryolar: ASSIST-01, 14, 22, 23.

using System.Net;
using AjandaAI.Application.Assistant;

namespace AjandaAI.IntegrationTests.Assistant;

public class AssistantNotConfiguredTests : AssistantTestBase, IClassFixture<UnconfiguredAssistantApiFactory>
{
    public AssistantNotConfiguredTests(UnconfiguredAssistantApiFactory factory) : base(factory) { }

    // ASSIST-01: anahtar yok → 503 "Asistan yapılandırılmadı."; diğer uçlar normal.
    [Fact]
    public async Task ASSIST_01_NoApiKey_Returns503NotConfigured_OtherEndpointsWork()
    {
        var user = await NewUserAsync();

        var response = await ChatAsync(user.Access);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(AssistantService.NotConfiguredMessage, (await ReadRootAsync(response)).GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(user.Access, HttpMethod.Get, "/api/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(user.Access, HttpMethod.Get, "/api/activities")).StatusCode);
    }
}

public class AssistantDisabledToolTests : AssistantTestBase, IClassFixture<DisabledToolAssistantApiFactory>
{
    public AssistantDisabledToolTests(DisabledToolAssistantApiFactory factory) : base(factory) { }

    // ASSIST-14: kapalı araç modele sunulmaz; çağrılmaya çalışılırsa reddedilir (dış servise istek gitmez).
    [Fact]
    public async Task ASSIST_14_DisabledTool_NotOffered_AndCallIsRejected()
    {
        var user = await NewUserAsync();
        Model.Script = messages => ScriptedChatClient.ToolResults(messages).Count == 0
            ? ScriptedChatClient.Call(AssistantToolNames.HavaDurumu)
            : ScriptedChatClient.Text("sonuc: " + string.Join(" ", ScriptedChatClient.ToolResults(messages)));

        var response = await ChatAsync(user.Access, "Yarın hava?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offered = Model.Requests.First().ToolNames;
        Assert.DoesNotContain(AssistantToolNames.HavaDurumu, offered);
        Assert.Contains(AssistantToolNames.HaftayiListele, offered);
        var data = await ReadDataAsync(response);
        Assert.DoesNotContain(AssistantToolNames.HavaDurumu, UsedTools(data));
        Assert.Contains("not found", data.GetProperty("reply").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Assistant.External.For(FakeExternalHandler.Weather));
    }
}

public class AssistantMinuteLimitTests : AssistantTestBase, IClassFixture<MinuteLimitedAssistantApiFactory>
{
    public AssistantMinuteLimitTests(MinuteLimitedAssistantApiFactory factory) : base(factory) { }

    // ASSIST-22: dakikada 11. mesaj 429; başka kullanıcı etkilenmez.
    [Fact]
    public async Task ASSIST_22_EleventhMessageInMinute_Returns429_OtherUserUnaffected()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await ChatAsync(user.Access)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await ChatAsync(user.Access)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChatAsync(other.Access)).StatusCode);
    }
}

public class AssistantDailyLimitTests : AssistantTestBase, IClassFixture<DailyLimitedAssistantApiFactory>
{
    public AssistantDailyLimitTests(DailyLimitedAssistantApiFactory factory) : base(factory) { }

    // ASSIST-23: günlük sınır (testte 3) aşılınca 429 ve Türkçe mesaj; diğer uçlar sınırdan etkilenmez.
    [Fact]
    public async Task ASSIST_23_DailyLimitExceeded_Returns429WithTurkishMessage()
    {
        var user = await NewUserAsync();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await ChatAsync(user.Access)).StatusCode);

        var limited = await ChatAsync(user.Access);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Contains("günde 200", (await ReadRootAsync(limited)).GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(user.Access, HttpMethod.Get, "/api/categories")).StatusCode);
    }
}
