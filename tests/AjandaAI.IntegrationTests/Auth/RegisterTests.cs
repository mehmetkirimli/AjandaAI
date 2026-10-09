// Kayıt (register) senaryoları: AUTH-01..06 (docs/auth-test-senaryolari.md).
// Gerçek HTTP + ajandaai_test veritabanı; mail gönderimi sahte IEmailSender ile gözlenir.
// Hesap sızdırmama: yeni ve kayıtlı e-posta yolları aynı 202 gövdesini döner.

using System.Net;
using System.Net.Http.Json;
using AjandaAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Auth;

public class RegisterTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public RegisterTests(AuthApiFactory factory) : base(factory)
    {
    }

    // AUTH-01
    [Fact]
    public async Task AUTH_01_Register_NewEmail_Returns202_CreatesUnverifiedUser_SendsVerificationMail()
    {
        var email = NewEmail();

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var user = await GetUserAsync(email);
        Assert.Null(user.EmailConfirmedAt);
        Assert.True(user.IsActive);
        var mail = await Factory.Emails.WaitForAsync(EmailKind.Verification, email);
        Assert.False(string.IsNullOrWhiteSpace(mail.Token));
    }

    // AUTH-02
    [Fact]
    public async Task AUTH_02_Register_WithRoleAdminInBody_CreatesUserWithUserRole()
    {
        var email = NewEmail();

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            DisplayName = DefaultDisplayName,
            TimeZoneId = "Europe/Istanbul",
            Password = DefaultPassword,
            Role = "Admin"
        }, Json);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(UserRole.User, (await GetUserAsync(email)).Role);
    }

    // AUTH-03
    [Fact]
    public async Task AUTH_03_Register_UnverifiedEmailAgain_HardDeletesOldUser_OldLinkInvalid()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var oldUser = await GetUserAsync(email);
        var oldToken = (await Factory.Emails.WaitForAsync(EmailKind.Verification, email, nth: 1)).Token!;

        var second = await RegisterAsync(email, "baska-bir-sifre-cumlesi-55");

        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Equal(1, await CountUsersAsync(email));
        var newUser = await GetUserAsync(email);
        Assert.NotEqual(oldUser.Id, newUser.Id);
        Assert.False(await Factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Id == oldUser.Id)));

        // Eski link geçersiz; yeni link çalışır.
        Assert.Equal(HttpStatusCode.BadRequest, (await VerifyAsync(oldToken)).StatusCode);
        var newToken = (await Factory.Emails.WaitForAsync(EmailKind.Verification, email, nth: 2)).Token!;
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(newToken)).StatusCode);
    }

    // AUTH-04
    [Fact]
    public async Task AUTH_04_Register_VerifiedActiveEmail_SameBody_NoNewUser_AccountExistsMail()
    {
        var freshResponse = await RegisterAsync(NewEmail());
        var freshBody = await freshResponse.Content.ReadAsStringAsync();

        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        var hashesBefore = Factory.Passwords.HashCount;

        var response = await RegisterAsync(email, "baska-bir-sifre-cumlesi-55");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(freshBody, await response.Content.ReadAsStringAsync()); // AUTH-01 ile aynı gövde
        Assert.Equal(1, await CountUsersAsync(email));
        Assert.Equal(userId, (await GetUserAsync(email)).Id);
        await Factory.Emails.WaitForAsync(EmailKind.AccountExists, email);
        Assert.Equal(1, Factory.Emails.Count(EmailKind.Verification, email)); // yeni doğrulama maili yok
        Assert.Equal(hashesBefore + 1, Factory.Passwords.HashCount); // timing eşitleme: kayıtlı yolda da hash
    }

    // AUTH-05
    [Fact]
    public async Task AUTH_05_Register_VerifiedButInactiveEmail_SameAsAuth04()
    {
        var freshBody = await (await RegisterAsync(NewEmail())).Content.ReadAsStringAsync();
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        var response = await RegisterAsync(email, "baska-bir-sifre-cumlesi-55");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(freshBody, await response.Content.ReadAsStringAsync());
        Assert.Equal(1, await CountUsersAsync(email));
        var user = await GetUserAsync(email);
        Assert.Equal(userId, user.Id);
        Assert.False(user.IsActive);
        await Factory.Emails.WaitForAsync(EmailKind.AccountExists, email);
    }

    // AUTH-06
    [Fact]
    public async Task AUTH_06_Register_EmailDifferingOnlyByCase_Unverified_ReplacesSingleUser()
    {
        var email = NewEmail();
        var upper = email.ToUpperInvariant();
        await RegisterAsync(upper);

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, await CountUsersAsync(email));
        Assert.Equal(email, (await GetUserAsync(email)).Email); // yeni kayıt eskisinin yerini aldı
    }

    // AUTH-06
    [Fact]
    public async Task AUTH_06_Register_EmailDifferingOnlyByCase_Verified_IsTreatedAsExistingAccount()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);

        var response = await RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, await CountUsersAsync(email));
        Assert.Equal(userId, (await GetUserAsync(email)).Id);
        await Factory.Emails.WaitForAsync(EmailKind.AccountExists, email);
    }
}
