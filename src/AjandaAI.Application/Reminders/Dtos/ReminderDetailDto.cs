// Tek bir Reminder kaydının detay okuma modelidir.
// ActivityTitle, bağlı Activity'nin Title alanıyla aynı tiptedir (string).

namespace AjandaAI.Application.Reminders.Dtos;

public record ReminderDetailDto(
    int Id,
    int ActivityId,
    string ActivityTitle,
    DateTimeOffset RemindAt,
    bool IsSent,
    DateTimeOffset? SentAt,
    string? Note,
    DateTimeOffset CreatedAt);
