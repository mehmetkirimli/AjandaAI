// Reminder kaynağının DI kayıtlarını yapan modüldür.
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.

using AjandaAI.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Reminders;

public class ReminderModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<ReminderService>();
    }
}
