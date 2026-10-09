// E-posta doğrulama senaryoları: AUTH-10..14 (AUTH-15 EnvironmentRegistrationTests'tedir).
// Doğrulama token'ı sahte IEmailSender'dan alınır; süre aşımı DB'de ExpiresAt geriye çekilerek simüle edilir.

using System.Net;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Auth;

public class EmailVerificationTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public EmailVerificationTests(AuthApiFactory factory) : base(factory)
    {
    }

    private async Task<(string Email, string Token)> RegisterPendingAsync()
    {
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Accepted, (await RegisterAsync(email)).StatusCode);
        var mail = await Factory.Emails.WaitForAsync(EmailKind.Verification, email);
        return (email, mail.Token!);
    }

    // AUTH-10
    [Fact]
    public async Task AUTH_10_VerifyEmail_ValidLink_SetsEmailConfirmedAt()
    {
        var (email, token) = await RegisterPendingAsync();

        var response = await VerifyAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull((await GetUserAsync(email)).EmailConfirmedAt);
    }

    // AUTH-11
    [Fact]
    public async Task AUTH_11_VerifyEmail_SameLinkTwice_SecondIsRejected()
    {
        var (_, token) = await RegisterPendingAsync();
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(token)).StatusCode);

        var second = await VerifyAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    // AUTH-12
    [Fact]
    public async Task AUTH_12_VerifyEmail_LinkOlderThan12Hours_IsRejected()
    {
        var (email, token) = await RegisterPendingAsync();
        var userId = (await GetUserAsync(email)).Id;
        await UpdateDbAsync(db => db.EmailVerificationTokens.Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));

        var response = await VerifyAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null((await GetUserAsync(email)).EmailConfirmedAt);
    }

    // AUTH-12
    [Fact]
    public async Task AUTH_12_VerificationToken_LifetimeIsTwelveHours()
    {
        var (email, _) = await RegisterPendingAsync();
        var userId = (await GetUserAsync(email)).Id;

        var row = await Factory.WithDbAsync(db => db.EmailVerificationTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));

        Assert.InRange((row.ExpiresAt - row.CreatedAt).TotalHours, 11.99, 12.01);
    }

    // AUTH-13
    [Fact]
    public async Task AUTH_13_VerificationToken_IsStoredAsSha256Hash_NotPlainText()
    {
        var (email, token) = await RegisterPendingAsync();
        var userId = (await GetUserAsync(email)).Id;

        var row = await Factory.WithDbAsync(db => db.EmailVerificationTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));

        Assert.NotEqual(token, row.TokenHash);
        Assert.Equal(Sha256Hex(token), row.TokenHash);
        Assert.Equal(64, row.TokenHash.Length);
    }

    // AUTH-14
    [Fact]
    public async Task AUTH_14_VerifyEmail_InactiveUser_DoesNotActivateAccount()
    {
        var (email, token) = await RegisterPendingAsync();
        var userId = (await GetUserAsync(email)).Id;
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        var response = await VerifyAsync(token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var user = await GetUserAsync(email);
        Assert.False(user.IsActive);
        Assert.Null(user.EmailConfirmedAt);
    }
}
