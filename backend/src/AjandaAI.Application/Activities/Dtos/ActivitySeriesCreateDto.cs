// Tekrarlayan aktivite serisi oluşturma isteğidir (ADR 0022): şablon alanları + tekrar kuralı.
// Start/End İLK oluşumun zamanıdır. Sahip body'den alınmaz, token'daki kullanıcıdır.
// Recurrence.DaysOfWeek Türkçe sıradadır: 1=Pzt ... 7=Paz (yalnızca Weekly'de kullanılır).

using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities.Dtos;

public record RecurrenceDto(RecurrenceFrequency Frequency, List<int>? DaysOfWeek, DateOnly Until);

public record ActivitySeriesCreateDto(
    int CategoryId,
    string Title,
    string Description,
    ActivityStatus Status,
    Priority Priority,
    EnergyLevel EnergyLevel,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string? Location,
    bool IsFlexible,
    decimal EstimatedBudget,
    RecurrenceDto? Recurrence,
    int? ReminderMinutesBefore);
