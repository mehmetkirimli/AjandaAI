// Category kaynağının DI kayıtlarını yapan modüldür (servis + validator'lar).
// Repository kaydı Infrastructure/DependencyInjection.cs içinde yapılır.

using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Categories.Validators;
using AjandaAI.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Categories;

public class CategoryModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<IValidator<CategoryCreateDto>, CategoryCreateDtoValidator>();
        services.AddScoped<IValidator<CategoryUpdateDto>, CategoryUpdateDtoValidator>();
    }
}
