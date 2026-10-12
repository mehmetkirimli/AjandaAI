// Aktivite detayında dönen seri özetidir (ADR 0022).
// DaysOfWeek Türkçe sıradadır: 1=Pzt ... 7=Paz; Weekly dışında boş liste olabilir.

namespace AjandaAI.Application.Activities.Dtos;

public record SeriesSummaryDto(int Id, string Frequency, List<int> DaysOfWeek, DateOnly StartDate, DateOnly EndDate);
