// Claude araçlarının (MCP, ADR 0024) okuma modelleri: hafta listesi ve boş zaman sorgusu.
// Tarihler kullanıcının saat diliminde yorumlanır; zamanlar o dilimin offset'iyle döner.

namespace AjandaAI.Application.Activities.Dtos;

public record AgendaWeekDto(DateOnly WeekStart, DateOnly WeekEnd, string TimeZoneId, IReadOnlyList<ActivityListDto> Activities);

public record FreeTimeQueryDto(DateOnly From, DateOnly To, int MinMinutes, TimeOnly WindowStart, TimeOnly WindowEnd);

public record FreeSlotDto(DateTimeOffset Start, DateTimeOffset End, int Minutes);
