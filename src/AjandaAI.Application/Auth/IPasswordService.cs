// Şifre hash'leme ve doğrulama sözleşmesi (ADR 0018 Karar 10). Implementasyon: Infrastructure/Auth/PasswordService.
// VerifyDummy, kullanıcı bulunamadığında yanıt süresini eşitlemek için aynı maliyette bir doğrulama çalıştırır.

namespace AjandaAI.Application.Auth;

public enum PasswordVerifyResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordService
{
    string Hash(string password);

    PasswordVerifyResult Verify(string passwordHash, string password);

    /// <summary>Sabit bir dummy hash'e karşı doğrulama yapar; sonuç atılır (timing eşitleme).</summary>
    void VerifyDummy(string password);
}
