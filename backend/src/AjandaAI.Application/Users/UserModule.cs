// User kaynağının DI kayıtlarını yapan modüldür. Kullanıcı yönetimi servisi Admin dilimindedir
// (AdminUserService); burada yalnızca onun kullandığı güncelleme validator'ı kayıtlıdır.
// IUserRepository kaydı Infrastructure/DependencyInjection.cs içinde yapılır (takım lideri).

using AjandaAI.Application.Common;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Application.Users.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Users;

public class UserModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateDtoValidator>();
    }
}
