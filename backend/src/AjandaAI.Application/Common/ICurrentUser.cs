// İsteği yapan kullanıcının kimliği (ADR 0019). Application HttpContext'i tanımaz;
// implementasyon Api katmanındadır (HttpCurrentUser).
// Kimlik bilgisi yoksa UserId okumak InvalidOperationException fırlatır: bu bir geliştirici
// hatasıdır ([AllowAnonymous] bir endpoint'ten çağrılmış), middleware 500'e çevirir.

namespace AjandaAI.Application.Common;

public interface ICurrentUser
{
    int UserId { get; }

    // Şu an Application'da kullanılmıyor: admin yetkisi controller'da [Authorize(Roles)] ile verilir ve
    // kullanıcı endpoint'lerinde admin istisnası yoktur (ADR 0018 Karar 3). ADR 0019 gereği sözleşmede durur.
    bool IsAdmin { get; }
}
