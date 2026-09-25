// Tüm migration zincirinin boş bir veritabanına baştan uygulanabildiğini doğrular.
// ajandaai_test'e dokunmaz: aynı sunucuda geçici bir veritabanı açar, sonunda siler.

using AjandaAI.Infrastructure.Persistence;
using AjandaAI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AjandaAI.IntegrationTests.Migrations;

public class MigrationTests : IClassFixture<AjandaApiFactory>
{
    private readonly AjandaApiFactory _factory;

    public MigrationTests(AjandaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllMigrations_ApplyToEmptyDatabase_AndSeedTenCategories()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_factory.ConnectionString)
        {
            Database = $"ajandaai_migration_{Guid.NewGuid():N}"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new AppDbContext(options);
        try
        {
            await db.Database.MigrateAsync();

            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(10, await db.Categories.CountAsync());
            Assert.Equal(Enumerable.Range(1, 10), await db.Categories.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync());
            Assert.True(await db.Categories.AllAsync(c => c.IsActive));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
