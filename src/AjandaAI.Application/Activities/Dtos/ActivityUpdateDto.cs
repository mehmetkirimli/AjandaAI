// Mevcut Activity'yi güncellemek için istemciden gelen yazma modelidir.
// Sahip (UserId) güncellemede değiştirilemez.
// Kurallar Validators/ActivityUpdateDtoValidator.cs içindedir.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities.Dtos;

public record ActivityUpdateDto(
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
