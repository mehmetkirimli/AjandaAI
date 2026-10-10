// Kullanıcının sistemdeki rolü (ADR 0018 Karar 5).
// Register her zaman User üretir; Admin DB üzerinden elle atanır.
// PostgreSQL'de string olarak saklanır.

namespace AjandaAI.Domain.Enums;

public enum UserRole
{
    User,
    Admin
}
