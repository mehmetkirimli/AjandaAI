// IPasswordService implementasyonu: PasswordHasher<T> (PBKDF2 + HMAC-SHA512, ASP.NET Identity'nin tamamı değil).
// SuccessRehashNeeded, eski algoritma/iterasyonla hash'lenmiş şifrenin yeniden hash'lenmesi gerektiğini bildirir.
// Dummy hash, aynı hasher ile üretilir: kullanıcı yokken yapılan doğrulama gerçek doğrulamayla aynı maliyettedir.

using AjandaAI.Application.Auth;
using AjandaAI.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AjandaAI.Infrastructure.Auth;

public sealed class PasswordService : IPasswordService
{
    // PasswordHasher kullanıcı nesnesini kullanmaz; yalnızca tip parametresi için gerekir.
    private static readonly User NoUser = new();

    private readonly PasswordHasher<User> _hasher = new();
    private readonly string _dummyHash;

    public PasswordService()
    {
        _dummyHash = _hasher.HashPassword(NoUser, Guid.NewGuid().ToString("N"));
    }

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public PasswordVerifyResult Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerifyResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerifyResult.SuccessRehashNeeded,
            _ => PasswordVerifyResult.Failed
        };

    public void VerifyDummy(string password) =>
        _hasher.VerifyHashedPassword(NoUser, _dummyHash, password);
}
