// API'yi Test ortamında (ajandaai_test) bellekte ayağa kaldıran factory'dir.
// Her test sınıfı kendi örneğini alır (IClassFixture); InitializeAsync şemayı günceller
// ve seed kategoriler hariç tüm tabloları TRUNCATE ... RESTART IDENTITY CASCADE ile temizler.

using AjandaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.IntegrationTests.Infrastructure;

public class AjandaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ExpectedDatabase = "ajandaai_test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Yanlış veritabanını (ör. Development) temizlemeye karşı korkuluk.
        var database = db.Database.GetDbConnection().Database;
        if (database != ExpectedDatabase)
            throw new InvalidOperationException(
                $"Entegrasyon testleri yalnızca {ExpectedDatabase} üzerinde çalışır; bağlanılan: {database}");

        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE reminders, activities, users RESTART IDENTITY CASCADE;");
    }

    public string ConnectionString
    {
        get
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()!;
        }
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
