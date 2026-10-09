// Sık kullanılan (sızmış) şifreler listesi sözleşmesi (ADR 0018 Karar 10).
// Karşılaştırma büyük/küçük harf duyarsızdır. Implementasyon Infrastructure/Auth'tadır.

namespace AjandaAI.Application.Auth;

public interface ICommonPasswordList
{
    bool Contains(string password);
}
