// users.email için DB seviyesindeki güvenlik ağı: validator/servis atlansa bile tekrarlı e-posta
// (büyük/küçük harf farkıyla da, ix_users_email_lower) unique index tarafından reddedilir.
// Eski UsersEndpointTests'ten taşındı; artık kaldırılan /api/users uçlarına bağlı değildir.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AjandaAI.IntegrationTests.Auth;

public class UserEmailIndexTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public UserEmailIndexTests(AuthApiFactory factory) : base(factory) { }

    private async Task AssertInsertRejectedAsync(string email)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            db.Users.Add(new User
            {
                Email = email, DisplayName = "Doğrudan", TimeZoneId = "Europe/Istanbul",
                PasswordHash = "x", CreatedAt = now, UpdatedAt = now
            });
            return await db.SaveChangesAsync();
        }));

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    // AUTH-06 (DB seviyesi)
    [Fact]
    public async Task DuplicateEmail_BypassingValidator_IsRejectedByUniqueIndex()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);

        await AssertInsertRejectedAsync(email);
        Assert.Equal(1, await CountUsersAsync(email));
    }

    // AUTH-06 (DB seviyesi)
    [Fact]
    public async Task DifferentCaseEmail_BypassingValidator_IsRejectedByUniqueIndex()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);

        await AssertInsertRejectedAsync(email.ToUpperInvariant());
        Assert.Equal(1, await CountUsersAsync(email));
    }
}
