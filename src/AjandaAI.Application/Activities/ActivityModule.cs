// Activity kaynağının DI kayıtlarını yapan modüldür.
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.

using AjandaAI.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Activities;

public class ActivityModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<ActivityService>();
    }
}
