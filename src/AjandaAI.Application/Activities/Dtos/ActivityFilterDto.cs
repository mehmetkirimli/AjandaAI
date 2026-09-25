// GET /api/activities için sayfalama + opsiyonel filtre parametreleridir ([FromQuery]).
// From/To, Start alanına göre kapalı aralık uygular; null alanlar filtrelenmez.
// Status ve Priority query string'de enum adı olarak gelir (örn. ?status=Planned).

using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities.Dtos;

public class ActivityFilterDto : PageRequest
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public ActivityStatus? Status { get; set; }

    public int? CategoryId { get; set; }

    public Priority? Priority { get; set; }
}
