// Email benzersizlik kısıtı veritabanında ihlal edildiğinde repository'nin fırlattığı hatadır.
// Validator'ın EmailExistsAsync kontrolü eşzamanlı isteklerde yarışı kaçırabilir; son savunma DB index'idir.
// Application EF Core'u tanımaz: Infrastructure, Postgres 23505 hatasını bu tipe çevirir.

namespace AjandaAI.Application.Users;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(Exception innerException)
        : base("Email benzersizlik kısıtı ihlal edildi.", innerException)
    {
    }
}
