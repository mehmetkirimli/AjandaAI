// Aktivitenin gerektirdiği enerji seviyesi.
// Gün içi yük dengeleme ve öneri üretiminde kullanılır.
// PostgreSQL'de string olarak saklanır.

namespace AjandaAI.Domain.Enums;

public enum EnergyLevel
{
    Low,
    Medium,
    High
}
