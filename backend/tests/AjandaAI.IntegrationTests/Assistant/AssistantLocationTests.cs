// Varsayılan konum (ürün sahibi kararı 2026-11-11): profil alanı ve asistan sistem talimatı.
// Senaryolar: ASSIST-60..63 (docs/asistan-test-senaryolari.md). Gerçek model çağrılmaz (ScriptedChatClient).

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AjandaAI.IntegrationTests.Assistant;

public class AssistantLocationTests : AssistantTestBase, IClassFixture<AssistantApiFactory>
{
    public AssistantLocationTests(AssistantApiFactory factory) : base(factory) { }

    private Task<HttpResponseMessage> PutMeAsync(string access, string? location) =>
        SendAsync(access, HttpMethod.Put, "/api/auth/me",
            new { displayName = "Konum Testi", timeZoneId = "Europe/Istanbul", location });

    private async Task<string?> GetLocationAsync(string access)
    {
        var me = await ReadDataAsync(await SendAsync(access, HttpMethod.Get, "/api/auth/me"));
        var location = me.GetProperty("location");
        return location.ValueKind == JsonValueKind.Null ? null : location.GetString();
    }

    private async Task<string> SystemPromptAsync(string access)
    {
        Model.Script = _ => ScriptedChatClient.Text("Tamam.");
        Assert.Equal(HttpStatusCode.OK, (await ChatAsync(access, "Yarın hava nasıl?")).StatusCode);
        var system = Model.Requests.Last().Messages[0];
        Assert.Equal(ChatRole.System, system.Role);
        return system.Text;
    }

    // ASSIST-60: konum girilir (trim), değiştirilir, boş gönderilince silinir.
    [Fact]
    public async Task ASSIST_60_Location_SetChangeClear_ViaAuthMe()
    {
        var user = await NewUserAsync();
        Assert.Null(await GetLocationAsync(user.Access));

        var put = await PutMeAsync(user.Access, "  Üsküdar, İstanbul  ");
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Üsküdar, İstanbul", (await ReadDataAsync(put)).GetProperty("location").GetString());
        Assert.Equal("Üsküdar, İstanbul", await GetLocationAsync(user.Access));

        Assert.Equal(HttpStatusCode.OK, (await PutMeAsync(user.Access, "Kadıköy, İstanbul")).StatusCode);
        Assert.Equal("Kadıköy, İstanbul", await GetLocationAsync(user.Access));

        Assert.Equal(HttpStatusCode.OK, (await PutMeAsync(user.Access, "   ")).StatusCode);
        Assert.Null(await GetLocationAsync(user.Access));

        await PutMeAsync(user.Access, "Ankara");
        Assert.Equal(HttpStatusCode.OK, (await PutMeAsync(user.Access, null)).StatusCode);
        Assert.Null(await GetLocationAsync(user.Access));
    }

    // ASSIST-61: 100 karakterden uzun konum 400 ve Türkçe mesaj; kayıt değişmez.
    [Fact]
    public async Task ASSIST_61_LocationTooLong_Returns400()
    {
        var user = await NewUserAsync();
        await PutMeAsync(user.Access, "Ankara");

        var response = await PutMeAsync(user.Access, new string('a', 101));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadRootAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Konum en fazla 100 karakter olabilir.", errors);
        Assert.Equal("Ankara", await GetLocationAsync(user.Access));
    }

    // ASSIST-62: konumu olan kullanıcının sistem talimatı konumu içerir.
    [Fact]
    public async Task ASSIST_62_SystemPrompt_ContainsLocation()
    {
        var user = await NewUserAsync();
        await PutMeAsync(user.Access, "Üsküdar, İstanbul");

        var prompt = await SystemPromptAsync(user.Access);

        Assert.Contains("Kullanıcının varsayılan konumu: Üsküdar, İstanbul.", prompt);
        Assert.Contains("konum belirtilmemişse bunu kullan", prompt);
    }

    // ASSIST-63: konumu olmayan kullanıcının sistem talimatında konum satırı yok.
    [Fact]
    public async Task ASSIST_63_SystemPrompt_WithoutLocation_HasNoLocationLine()
    {
        var user = await NewUserAsync();

        var prompt = await SystemPromptAsync(user.Access);

        Assert.DoesNotContain("varsayılan konumu", prompt);
    }
}
