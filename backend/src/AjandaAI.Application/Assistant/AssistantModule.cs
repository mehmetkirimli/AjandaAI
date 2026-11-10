// Asistan kaynağının DI kayıtları (servis, araç kayıt defteri, validator). Model (IAssistantModel) ve dış servis
// istemcileri Infrastructure/DependencyInjection.cs'tedir. Araç ayarı orada config'ten kurulmazsa hepsi açıktır.

using AjandaAI.Application.Assistant.Dtos;
using AjandaAI.Application.Assistant.Validators;
using AjandaAI.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AjandaAI.Application.Assistant;

public class AssistantModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new AssistantToolOptions());
        services.AddScoped<AssistantToolRegistry>();
        services.AddScoped<AssistantService>();
        services.AddScoped<IValidator<AssistantChatRequestDto>, AssistantChatRequestDtoValidator>();
    }
}
