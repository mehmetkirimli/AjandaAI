// Admin servislerinin DI kayıtları. IAdminRepository kaydı Infrastructure/DependencyInjection.cs içindedir.
// Validator'lar Auth (RegisterDto) ve Users (UserUpdateDto) modüllerinde kayıtlıdır, burada tekrar edilmez.

using AjandaAI.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Admin;

public class AdminModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<AdminUserService>();
        services.AddScoped<AdminContentService>();
    }
}
