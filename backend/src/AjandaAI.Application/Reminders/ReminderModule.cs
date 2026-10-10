// Reminder kaynağının DI kayıtlarını yapan modüldür.
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.
// TimeProvider TryAdd ile eklenir; başka bir kayıt varsa ezilmez.

using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Reminders.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AjandaAI.Application.Reminders;

public class ReminderModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IValidator<ReminderCreateDto>, ReminderCreateDtoValidator>();
        services.AddScoped<IValidator<ReminderUpdateDto>, ReminderUpdateDtoValidator>();
        services.AddScoped<ReminderService>();
    }
}
