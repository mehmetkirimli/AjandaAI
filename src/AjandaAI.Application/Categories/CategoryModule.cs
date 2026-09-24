// Category kaynağının DI kayıtlarını yapan modüldür.
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.

using AjandaAI.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Categories;

public class CategoryModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
    }
}
