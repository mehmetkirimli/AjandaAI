// Kişisel erişim anahtarı (PAT) uçları ve ValidateAsync (ADR 0024 §1, docs/mcp-test-senaryolari.md).
// Uç testleri gerçek HTTP + PostgreSQL; ValidateAsync testleri servisi DI scope'undan alır (HTTP'den bağımsızdır).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.Application.Auth;
using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.IntegrationTests.Auth;

public class PersonalAccessTokenTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public PersonalAccessTokenTests(AuthApiFactory factory) : base(factory)
    {
    }

    private async Task<(int Id, string Access)> NewUserAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        return (id, access);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string bearer, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> CreateAsync(string access, string? name = "Laptop") =>
        SendAsync(HttpMethod.Post, "/api/auth/tokens", access, new { Name = name });

    private async Task<(int Id, string Token)> CreateOkAsync(string access, string name = "Laptop")
    {
        var response = await CreateAsync(access, name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("token").GetString()!);
    }

    private Task<PersonalAccessToken> GetPatAsync(int id) =>
        Factory.WithDbAsync(db => db.PersonalAccessTokens.AsNoTracking().SingleAsync(t => t.Id == id));

    private async Task<PersonalAccessTokenIdentity?> ValidateAsync(string? raw)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PersonalAccessTokenService>().ValidateAsync(raw);
    }

    // MCP-01: 201; düz anahtar yalnızca bu yanıtta; DB'de SHA-256 özeti + ilk 8 karakter.
    [Fact]
    public async Task MCP01_Create_Returns201_RawTokenOnce_DbStoresHashAndPrefix()
    {
        var user = await NewUserAsync();
        var response = await CreateAsync(user.Access, "Laptop");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        var raw = data.GetProperty("token").GetString()!;
        Assert.StartsWith("ajd_", raw);
        Assert.Equal(47, raw.Length);
        Assert.Equal("Laptop", data.GetProperty("name").GetString());
        Assert.Equal(raw[..8], data.GetProperty("prefix").GetString());

        var stored = await GetPatAsync(data.GetProperty("id").GetInt32());
        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal(Sha256Hex(raw), stored.TokenHash);
        Assert.Equal(raw[..8], stored.Prefix);
        Assert.NotEqual(raw, stored.TokenHash);
    }

    // MCP-02: liste ad, prefix ve tarihleri döner; düz anahtar ve özet dönmez.
    [Fact]
    public async Task MCP02_List_ReturnsMetadata_WithoutRawTokenOrHash()
    {
        var user = await NewUserAsync();
        var (_, raw) = await CreateOkAsync(user.Access, "Laptop");

        var response = await SendAsync(HttpMethod.Get, "/api/auth/tokens", user.Access);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(raw, body);
        Assert.DoesNotContain(Sha256Hex(raw), body);
        var item = Assert.Single((await ReadDataAsync(response)).EnumerateArray());
        Assert.Equal("Laptop", item.GetProperty("name").GetString());
        Assert.Equal(raw[..8], item.GetProperty("prefix").GetString());
        Assert.True(item.TryGetProperty("createdAt", out _));
        Assert.True(item.TryGetProperty("expiresAt", out _));
        Assert.True(item.TryGetProperty("lastUsedAt", out _));
        Assert.False(item.TryGetProperty("token", out _));
        Assert.False(item.TryGetProperty("tokenHash", out _));
    }

    // MCP-03: 6. etkin anahtar 400 (Türkçe); iptal edilmiş ve süresi dolmuşlar sayılmaz.
    [Fact]
    public async Task MCP03_SixthActiveToken_Returns400_RevokedAndExpiredNotCounted()
    {
        var user = await NewUserAsync();
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
            ids.Add((await CreateOkAsync(user.Access, $"Anahtar {i}")).Id);

        var sixth = await CreateAsync(user.Access, "Altıncı");
        Assert.Equal(HttpStatusCode.BadRequest, sixth.StatusCode);
        Assert.Equal(PersonalAccessTokenService.LimitMessage, (await ReadRootAsync(sixth)).GetProperty("message").GetString());

        // Birini iptal et, birinin süresini doldur: iki yer açılır.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/auth/tokens/{ids[0]}", user.Access)).StatusCode);
        await UpdateDbAsync(db => db.PersonalAccessTokens.Where(t => t.Id == ids[1])
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));

        await CreateOkAsync(user.Access, "Yeni 1");
        await CreateOkAsync(user.Access, "Yeni 2");
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(user.Access, "Fazla")).StatusCode);
    }

    // MCP-04: başkasının veya olmayan anahtarı iptal etmek 404; anahtar etkin kalır.
    [Fact]
    public async Task MCP04_RevokeOthersToken_Returns404_AndTokenStaysActive()
    {
        var owner = await NewUserAsync();
        var other = await NewUserAsync();
        var (id, raw) = await CreateOkAsync(owner.Access);

        var response = await SendAsync(HttpMethod.Delete, $"/api/auth/tokens/{id}", other.Access);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Delete, "/api/auth/tokens/999999999", other.Access)).StatusCode);

        Assert.Null((await GetPatAsync(id)).RevokedAt);
        Assert.NotNull(await ValidateAsync(raw));
    }

    // MCP-05: PAT, Authorization: Bearer olarak normal API'de geçmez; yeni anahtar üretilemez.
    [Fact]
    public async Task MCP05_CreateTokenWithPat_Returns401()
    {
        var user = await NewUserAsync();
        var (_, raw) = await CreateOkAsync(user.Access);

        var response = await SendAsync(HttpMethod.Post, "/api/auth/tokens", raw, new { Name = "Saldırı" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var count = await Factory.WithDbAsync(db => db.PersonalAccessTokens.CountAsync(t => t.UserId == user.Id));
        Assert.Equal(1, count);
    }

    // MCP-06: ad boş veya 100 karakterden uzun → 400.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("101")]
    public async Task MCP06_InvalidName_Returns400(string? name)
    {
        var user = await NewUserAsync();
        if (name == "101")
            name = new string('a', 101);

        var response = await CreateAsync(user.Access, name);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // MCP-06 sınırı: tam 100 karakter kabul edilir.
    [Fact]
    public async Task MCP06_NameOf100Chars_IsAccepted()
    {
        var user = await NewUserAsync();
        await CreateOkAsync(user.Access, new string('a', 100));
    }

    // MCP-07: süre verilmez; ExpiresAt = şimdi + 90 gün.
    [Fact]
    public async Task MCP07_ExpiresAt_IsNowPlus90Days()
    {
        var user = await NewUserAsync();
        var before = DateTimeOffset.UtcNow;
        var (id, _) = await CreateOkAsync(user.Access);
        var after = DateTimeOffset.UtcNow;

        var stored = await GetPatAsync(id);
        Assert.InRange(stored.ExpiresAt, before.AddDays(90).AddSeconds(-1), after.AddDays(90).AddSeconds(1));
        Assert.Equal(stored.CreatedAt.AddDays(90), stored.ExpiresAt);
    }

    // ValidateAsync başarılı yolu (MCP-10'un servis tarafı): kimlik döner, LastUsedAt güncellenir.
    [Fact]
    public async Task Validate_ValidToken_ReturnsIdentity_AndUpdatesLastUsedAt()
    {
        var user = await NewUserAsync();
        var (id, raw) = await CreateOkAsync(user.Access);
        Assert.Null((await GetPatAsync(id)).LastUsedAt);

        var identity = await ValidateAsync(raw);

        Assert.NotNull(identity);
        Assert.Equal(user.Id, identity.UserId);
        Assert.Equal(id, identity.TokenId);
        Assert.NotNull((await GetPatAsync(id)).LastUsedAt);
    }

    // MCP-12: uydurma / biçimi bozuk anahtar → null.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ajd_")]
    [InlineData("ajd_kisa")]
    [InlineData("xyz_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("ajd_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA!")]
    [InlineData("ajd_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task MCP12_FakeOrMalformedToken_ReturnsNull(string? raw)
    {
        Assert.Null(await ValidateAsync(raw));
    }

    // MCP-13: iptal edilmiş anahtar → null.
    [Fact]
    public async Task MCP13_RevokedToken_ReturnsNull()
    {
        var user = await NewUserAsync();
        var (id, raw) = await CreateOkAsync(user.Access);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/auth/tokens/{id}", user.Access)).StatusCode);

        Assert.NotNull((await GetPatAsync(id)).RevokedAt);
        Assert.Null(await ValidateAsync(raw));
    }

    // MCP-14: süresi dolmuş anahtar → null.
    [Fact]
    public async Task MCP14_ExpiredToken_ReturnsNull()
    {
        var user = await NewUserAsync();
        var (id, raw) = await CreateOkAsync(user.Access);
        await UpdateDbAsync(db => db.PersonalAccessTokens.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(-1))));

        Assert.Null(await ValidateAsync(raw));
    }

    // MCP-15: pasife alınmış kullanıcının anahtarı → null.
    [Fact]
    public async Task MCP15_InactiveUsersToken_ReturnsNull()
    {
        var user = await NewUserAsync();
        var (_, raw) = await CreateOkAsync(user.Access);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        Assert.Null(await ValidateAsync(raw));
    }

    // MCP-18: şifre değişince anahtarlar geçerli kalır (ADR 0024 §1, bilinçli).
    [Fact]
    public async Task MCP18_PasswordChange_TokenStaysValid()
    {
        var user = await NewUserAsync();
        var (_, raw) = await CreateOkAsync(user.Access);

        var change = await SendAsync(HttpMethod.Post, "/api/auth/change-password", user.Access,
            new { CurrentPassword = DefaultPassword, NewPassword = "yesil-orman-yolunda-42" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var identity = await ValidateAsync(raw);
        Assert.NotNull(identity);
        Assert.Equal(user.Id, identity.UserId);
    }
}
