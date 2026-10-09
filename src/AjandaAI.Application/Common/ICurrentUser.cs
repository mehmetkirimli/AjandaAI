// İsteği yapan kullanıcının kimliği (ADR 0019). Application HttpContext'i tanımaz;
// implementasyon Api katmanındadır (HttpCurrentUser).
// Kimlik bilgisi yoksa UserId okumak InvalidOperationException fırlatır: bu bir geliştirici
// hatasıdır ([AllowAnonymous] bir endpoint'ten çağrılmış), middleware 500'e çevirir.

namespace AjandaAI.Application.Common;

public interface ICurrentUser
{
    int UserId { get; }

    bool IsAdmin { get; }
}
