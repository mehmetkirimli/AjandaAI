// GET /api/reminders için sayfalama + opsiyonel filtre parametreleridir ([FromQuery]).
// Null alanlar filtrelenmez.

using AjandaAI.Application.Common;

namespace AjandaAI.Application.Reminders.Dtos;

public class ReminderFilterDto : PageRequest
{
    public int? ActivityId { get; set; }

    public bool? IsSent { get; set; }
}
