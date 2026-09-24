// Yeni Activity oluşturmak için istemciden gelen yazma modelidir.
// Kurallar Validators/ActivityCreateDtoValidator.cs içindedir.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities.Dtos;

public record ActivityCreateDto(
    int UserId,
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
    int? Rating,
    bool? WouldRepeat);
