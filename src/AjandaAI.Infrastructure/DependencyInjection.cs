// Infrastructure katmanının composition root'a sunduğu tek giriş noktasıdır.
// Program.cs yalnızca AddInfrastructure(...) çağırır, iç sınıfları tanımaz.
// DbContext, repository'ler ve Application'daki tüm IModule'ler burada kaydedilir.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Admin;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders;
using AjandaAI.Application.Users;
using AjandaAI.Infrastructure.Auth;
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

        services.AddSingleton<SlowQueryInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>()));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ICommonPasswordList, CommonPasswordList>();

        AddModules(services);

        return services;
    }

    /// <summary>
    /// Yalnızca Development'ta çağrılır: doğrulama linkini log'a yazan LogEmailSender (ADR 0018).
    /// Ortam kararı Program.cs'te kalır, ama Api Infrastructure'ın iç sınıfını tanımaz (ADR 0012).
    /// Production'da çağrılmaz: LogEmailSender log'a gizli link yazar (AUTH-15).
    /// </summary>
    public static IServiceCollection AddDevelopmentInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, LogEmailSender>();
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
