// Activity listesinde dönen özet okuma modelidir.
// Ham entity yerine bu DTO döner; navigation property içermez.
// Enum alanları string olarak taşınır.

namespace AjandaAI.Application.Activities.Dtos;

public record ActivityListDto(
    int Id,
    int CategoryId,
    string Title,
    string Status,
    string Priority,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay);
