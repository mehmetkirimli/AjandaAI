// Activity okuma senaryolarını yöneten uygulama servisidir.
// Entity'leri DTO'ya çevirir; enum alanları string olarak döner.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Activities;

public class ActivityService
{
    private readonly IActivityRepository _repository;

    public ActivityService(IActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ActivityListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var activities = await _repository.GetAllAsync(cancellationToken);
        return activities.Select(ToListDto).ToList();
    }

    public async Task<ActivityDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        return activity is null ? null : ToDetailDto(activity);
    }

    private static ActivityListDto ToListDto(Activity a) => new(
        a.Id, a.CategoryId, a.Title, a.Status.ToString(), a.Priority.ToString(),
        a.Start, a.End, a.IsAllDay);

    private static ActivityDetailDto ToDetailDto(Activity a) => new(
        a.Id, a.UserId, a.CategoryId, a.Title, a.Description,
        a.Status.ToString(), a.Priority.ToString(), a.EnergyLevel.ToString(),
        a.Start, a.End, a.IsAllDay, a.Location, a.IsFlexible, a.EstimatedBudget,
        a.Rating, a.WouldRepeat, a.CreatedAt, a.UpdatedAt);
}
