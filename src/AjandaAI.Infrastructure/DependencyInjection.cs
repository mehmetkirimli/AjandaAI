// Infrastructure katmanının composition root'a sunduğu tek giriş noktasıdır.
// Program.cs yalnızca AddInfrastructure(...) çağırır, iç sınıfları tanımaz.
// Burada DbContext Npgsql sağlayıcısı ile kaydedilir.

using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        return services;
    }
}
