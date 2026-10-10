// Auth akışlarının DI kayıtlarını yapan modüldür (servis + validator).
// Repository ve Infrastructure/Auth servisleri Infrastructure/DependencyInjection.cs içinde kaydedilir.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Auth.Validators;
using AjandaAI.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Auth;

public class AuthModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<MeService>();
        services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
    }
}
