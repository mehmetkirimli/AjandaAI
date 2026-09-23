// Reminder kaynağının liste uç noktasında dönen okuma modelidir.
// Ham entity yerine bu DTO döner; navigation property içermez.

namespace AjandaAI.Application.Reminders.Dtos;

public record ReminderListDto(int Id, int ActivityId, DateTimeOffset RemindAt, bool IsSent);
