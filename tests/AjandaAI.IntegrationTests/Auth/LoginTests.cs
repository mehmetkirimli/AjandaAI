// Login senaryoları: AUTH-20..26 (AUTH-27 rate limit ayrı sınıftadır).
// Hata durumlarında gövdenin birebir aynı olması hesap sızdırmamayı doğrular; timing eşitleme
// süre ölçmeden, dummy doğrulamanın çalıştığı sayaçla gösterilir.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.Application.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AjandaAI.IntegrationTests.Auth;

public class LoginTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string FailedMessage = "E-posta veya şifre hatalı.";

    public LoginTests(AuthApiFactory factory) : base(factory)
    {
    }

    // AUTH-20
    [Fact]
    public async Task AUTH_20_Login_ValidCredentials_VerifiedUser_ReturnsAccessAndRefreshToken()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (access, refresh) = await ReadTokensAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(refresh));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(access);
        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("User", jwt.Claims.Single(c => c.Type == "role").Value);
    }

    // AUTH-21
    [Fact]
    public async Task AUTH_21_Login_WrongPassword_Returns401_WithGenericMessage()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);

        var response = await LoginAsync(email, "yanlis-sifre-denemesi-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var root = await ReadRootAsync(response);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(FailedMessage, root.GetProperty("message").GetString());
    }

    // AUTH-22
    [Fact]
    public async Task AUTH_22_Login_UnknownEmail_HasSameBodyAsWrongPassword()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var wrongPassword = await LoginAsync(email, "yanlis-sifre-denemesi-1");

        var unknown = await LoginAsync(NewEmail(), "yanlis-sifre-denemesi-1");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
    }

    // AUTH-23
    [Fact]
    public async Task AUTH_23_Login_UnknownEmail_StillRunsPasswordVerification_WithDummyHash()
    {
        var dummyBefore = Factory.Passwords.DummyVerifyCount;

        var response = await LoginAsync(NewEmail(), "yanlis-sifre-denemesi-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(dummyBefore + 1, Factory.Passwords.DummyVerifyCount);
    }

    // AUTH-24
    [Fact]
    public async Task AUTH_24_Login_UnverifiedUser_IsRejected_WithSameBody()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var reference = await LoginAsync(NewEmail());

        var response = await LoginAsync(email); // şifre doğru, e-posta doğrulanmamış

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(await reference.Content.ReadAsStringAsync(), await response.Content.ReadAsStringAsync());
    }

    // AUTH-25
    [Fact]
    public async Task AUTH_25_Login_InactiveUser_IsRejected_WithSameBody()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));
        var reference = await LoginAsync(NewEmail());

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(await reference.Content.ReadAsStringAsync(), await response.Content.ReadAsStringAsync());
    }

    // AUTH-26
    [Fact]
    public async Task AUTH_26_Login_OldAlgorithmHash_IsRehashedOnSuccess()
    {
        var email = NewEmail();
        var oldHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
        }));
        var oldHash = oldHasher.HashPassword(new User(), DefaultPassword);
        await Factory.WithDbAsync(async db =>
        {
            db.Users.Add(new User
            {
                Email = email,
                DisplayName = DefaultDisplayName,
                TimeZoneId = "Europe/Istanbul",
                PasswordHash = oldHash,
                EmailConfirmedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            return await db.SaveChangesAsync();
        });
        var currentHasher = new PasswordHasher<User>();
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded,
            currentHasher.VerifyHashedPassword(new User(), oldHash, DefaultPassword));

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newHash = (await GetUserAsync(email)).PasswordHash;
        Assert.NotEqual(oldHash, newHash);
        Assert.Equal(PasswordVerificationResult.Success,
            currentHasher.VerifyHashedPassword(new User(), newHash, DefaultPassword));
        // Sonraki giriş de çalışır.
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email)).StatusCode);
    }

    // AUTH-20
    [Fact]
    public async Task AUTH_20_Login_ReadsRoleFromDatabase_AdminRoleInAccessToken()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));

        var (access, _) = await LoginOkAsync(email);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(access);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
    }
}
