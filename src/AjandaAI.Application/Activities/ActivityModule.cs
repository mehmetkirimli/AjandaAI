// Activity kaynağının DI kayıtlarını (servis + validator'lar) yapan modüldür.
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Activities.Validators;
using AjandaAI.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Activities;

public class ActivityModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<ActivityService>();
        services.AddScoped<IValidator<ActivityCreateDto>, ActivityCreateDtoValidator>();
        services.AddScoped<IValidator<ActivityUpdateDto>, ActivityUpdateDtoValidator>();
    }
}
