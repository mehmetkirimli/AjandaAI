// Infrastructure katmanının composition root'a sunduğu tek giriş noktasıdır.
// Program.cs yalnızca AddInfrastructure(...) çağırır, iç sınıfları tanımaz.
// DbContext, repository'ler ve Application'daki tüm IModule'ler burada kaydedilir.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Catalog;
using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders;
using AjandaAI.Infrastructure.Persistence;
using AjandaAI.Infrastructure.Repositories;
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

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        AddModules(services);

        return services;
    }

    private static void AddModules(IServiceCollection services)
    {
        var moduleTypes = typeof(IModule).Assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type)!;
            module.Register(services);
        }
    }
}
