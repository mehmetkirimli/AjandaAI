// Reminder okuma senaryolarını yöneten uygulama servisidir.
// Liste özet DTO, Id ile sorgu bağlı aktivite başlığını içeren detay DTO döner.

using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Reminders;

public class ReminderService
{
    private readonly IReminderRepository _repository;

    public ReminderService(IReminderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ReminderListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reminders = await _repository.GetAllAsync(cancellationToken);
        return reminders.Select(r => new ReminderListDto(r.Id, r.ActivityId, r.RemindAt, r.IsSent)).ToList();
    }

    public async Task<ReminderDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var reminder = await _repository.GetByIdAsync(id, cancellationToken);
        return reminder is null ? null : ToDetailDto(reminder);
    }

    private static ReminderDetailDto ToDetailDto(Reminder r) => new(
        r.Id, r.ActivityId, r.Activity.Title, r.RemindAt, r.IsSent, r.SentAt, r.Note, r.CreatedAt);
}
