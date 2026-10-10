// Şifre kuralları senaryoları: AUTH-60..68 (ADR 0018 Karar 10), register endpoint'i üzerinden.
// Reddedilen şifreler 400 döner ve kullanıcı oluşmaz; kabul edilenler 202 döner. AUTH-67/68 ayrıca
// şifrenin Trim() edilmediğini ve PBKDF2'nin tüm uzunluğu kullandığını (72 byte sınırı yok) login ile gösterir.

using System.Net;
using AjandaAI.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AjandaAI.IntegrationTests.Auth;

public class PasswordRulesTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public PasswordRulesTests(AuthApiFactory factory) : base(factory)
    {
    }

    private async Task AssertRejectedAsync(string email, string password, string displayName = DefaultDisplayName)
    {
        var response = await RegisterAsync(email, password, displayName);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(await FindUserAsync(email));
    }

    private async Task AssertAcceptedAsync(string email, string password, string displayName = DefaultDisplayName)
    {
        var response = await RegisterAsync(email, password, displayName);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(await FindUserAsync(email));
    }

    // AUTH-60
    [Fact]
    public async Task AUTH_60_Password_NineCharacters_IsRejected_WithRuleMessage()
    {
        var email = NewEmail();

        var response = await RegisterAsync(email, "uzunluk9!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadRootAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Şifreniz en az 10 karakter olmalı. Uzun bir cümle kullanabilirsiniz. Çok yaygın şifreler kabul edilmez.", errors);
        Assert.Null(await FindUserAsync(email));
    }

    // AUTH-61
    [Fact]
    public async Task AUTH_61_Password_TenLowercaseCharacters_IsAccepted_NoComplexityRule()
    {
        await AssertAcceptedAsync(NewEmail(), "ajandamsin");
    }

    // AUTH-62
    [Fact]
    public async Task AUTH_62_Password_128Accepted_129Rejected()
    {
        await AssertAcceptedAsync(NewEmail(), string.Concat(Enumerable.Repeat("kalem", 26))[..128]);
        await AssertRejectedAsync(NewEmail(), string.Concat(Enumerable.Repeat("kalem", 26))[..129]);
    }

    // AUTH-63
    [Fact]
    public async Task AUTH_63_Password_InCommonList_IsRejected()
    {
        await AssertRejectedAsync(NewEmail(), "Password1!");
        await AssertRejectedAsync(NewEmail(), "password1!"); // büyük/küçük harf duyarsız
    }

    // AUTH-64
    [Fact]
    public async Task AUTH_64_Password_ContainingEmailLocalPart_IsRejected()
    {
        await AssertRejectedAsync("mehmet.kirimli@test.com", "benim-mehmet.kirimli-sifrem");
    }

    // AUTH-65
    [Fact]
    public async Task AUTH_65_Password_ContainingDisplayName_IsRejected()
    {
        await AssertRejectedAsync(NewEmail(), "zeynep arslan 12345", "Zeynep Arslan");
        await AssertRejectedAsync(NewEmail(), "arslan-ailesi-2024", "Zeynep Arslan"); // adın bir kelimesi
    }

    // AUTH-66
    [Fact]
    public async Task AUTH_66_Password_TurkishSentenceWithSpaces_IsAccepted_AndCanLogin()
    {
        var email = NewEmail();
        const string password = "ajandam benim en iyi arkadaşım";

        await RegisterAndVerifyAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, password)).StatusCode);
    }

    // AUTH-67
    [Fact]
    public async Task AUTH_67_Password_LeadingAndTrailingSpaces_AreStoredAsIs_TrimmedFormCannotLogin()
    {
        var email = NewEmail();
        const string password = "  kenarda bosluklar var  ";

        await RegisterAndVerifyAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, password.Trim())).StatusCode);
        var hash = (await GetUserAsync(email)).PasswordHash;
        var hasher = new PasswordHasher<User>();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(new User(), hash, password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(new User(), hash, password.Trim()));
    }

    // AUTH-68
    [Fact]
    public async Task AUTH_68_Password_Differing_AfterFirst72Bytes_SecondCannotLogin()
    {
        var email = NewEmail();
        var prefix = new string('k', 72);
        var registered = prefix + "AAAA";
        var other = prefix + "BBBB";
        await RegisterAndVerifyAsync(email, registered);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, registered)).StatusCode);
    }
}
