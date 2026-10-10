// timestamptz kolonları için DateTimeOffset dönüştürücüsü: yazarken değeri UTC'ye çevirir.
// Npgsql, timestamptz'ye yalnızca offset'i 0 olan DateTimeOffset yazar; "+03:00" gibi bir değer
// "Cannot write DateTimeOffset with Offset=03:00" hatasıyla 500'e dönerdi (F1 bulgusu, ADR 0004).
// Değerin temsil ettiği AN değişmez; yalnızca gösterimi UTC olur. Okuma zaten UTC döner.

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AjandaAI.Infrastructure.Persistence;

public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v)
    {
    }
}
