// Yeni Reminder oluşturma isteğinin girdi modelidir.
// Doğrulama kuralları Validators/ReminderCreateDtoValidator.cs içindedir.

namespace AjandaAI.Application.Reminders.Dtos;

public record ReminderCreateDto(int ActivityId, DateTimeOffset RemindAt, string? Note);
