// Resmî tatiller (Nager.Date, ülke TR; ADR 0025 §3). Sonuç yıl bazında önbelleğe alınır (ASSIST-17).

namespace AjandaAI.Application.Assistant.External;

public record HolidayDto(DateOnly Date, string LocalName, string Name);

public interface IHolidayClient
{
    Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(int year, CancellationToken cancellationToken = default);
}
