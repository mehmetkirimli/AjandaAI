// Tek bir Activity kaydının tüm alanlarını taşıyan okuma modelidir.
// Ham entity yerine bu DTO döner; navigation property içermez.
// Enum alanları string olarak taşınır.

namespace AjandaAI.Application.Activities.Dtos;

public record ActivityDetailDto(
    int Id,
    int UserId,
    int CategoryId,
    string Title,
    string Description,
    string Status,
    string Priority,
    string EnergyLevel,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string? Location,
    bool IsFlexible,
    decimal EstimatedBudget,
    int? Rating,
    bool? WouldRepeat,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
