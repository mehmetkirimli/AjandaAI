// Her kaynağın kendi servis kayıtlarını yaptığı modül sözleşmesidir.
// Infrastructure/DependencyInjection.cs assembly'deki tüm IModule'leri bulup çağırır.
// Repository kayıtları burada değil, Infrastructure tarafında yapılır.

using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Application.Common;

public interface IModule
{
    void Register(IServiceCollection services);
}
