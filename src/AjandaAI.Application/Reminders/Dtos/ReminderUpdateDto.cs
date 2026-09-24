// Var olan bir Reminder kaydını güncelleme isteğinin girdi modelidir.
// Doğrulama kuralları Validators/ReminderUpdateDtoValidator.cs içindedir.

namespace AjandaAI.Application.Reminders.Dtos;

public record ReminderUpdateDto(int ActivityId, DateTimeOffset RemindAt, string? Note);
