// Claude araçları için okuma odaklı ajanda sorguları (ADR 0024): bir haftanın aktiviteleri ve boş zaman.
// Gün ve hafta sınırları kullanıcının saat dilimindedir (User.TimeZoneId); hafta Pazartesi başlar.
// Boş zaman hesabında iptal edilen ve tüm gün süren aktiviteler meşgul sayılmaz (takvim uygulamalarındaki gibi).

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities;

public class AgendaService
{
    public const int MaxFreeTimeRangeDays = 31;

    private readonly IActivityRepository _repository;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;

    public AgendaService(IActivityRepository repository, IUserRepository users, ICurrentUser currentUser)
    {
        _repository = repository;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<AgendaWeekDto>> GetWeekAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var zone = await GetZoneAsync(cancellationToken);
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var activities = await LoadAsync(
            RecurrenceExpander.StartOfLocalDayUtc(monday, zone),
            RecurrenceExpander.StartOfLocalDayUtc(monday.AddDays(7), zone),
            cancellationToken);

        var list = activities.Select(a => ToListDto(a, zone)).ToList();
        return ApiResponse<AgendaWeekDto>.Ok(new AgendaWeekDto(monday, monday.AddDays(6), zone.Id, list));
    }

    public async Task<ApiResponse<List<FreeSlotDto>>> FindFreeTimeAsync(FreeTimeQueryDto query, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (query.To < query.From)
            errors.Add("Bitiş günü başlangıç gününden önce olamaz.");
        else if (query.To.DayNumber - query.From.DayNumber + 1 > MaxFreeTimeRangeDays)
            errors.Add($"En fazla {MaxFreeTimeRangeDays} günlük aralık sorgulanabilir.");
        if (query.MinMinutes is < 1 or > 1440)
            errors.Add("En az süre 1 ile 1440 dakika arasında olmalıdır.");
        if (query.WindowEnd <= query.WindowStart)
            errors.Add("Gün içi pencerenin bitişi başlangıcından sonra olmalıdır.");
        if (errors.Count > 0)
            return ApiResponse<List<FreeSlotDto>>.Fail("Doğrulama hatası.", errors);

        var zone = await GetZoneAsync(cancellationToken);
        // Önceki günden taşan (gece yarısını aşan) aktiviteler de görülsün diye bir gün geriden başlanır.
        var activities = await LoadAsync(
            RecurrenceExpander.StartOfLocalDayUtc(query.From.AddDays(-1), zone),
            RecurrenceExpander.StartOfLocalDayUtc(query.To.AddDays(1), zone),
            cancellationToken);

        var busy = activities
            .Where(a => !a.IsAllDay && a.Status != ActivityStatus.Cancelled)
            .Select(a => (a.Start, a.End));
        return ApiResponse<List<FreeSlotDto>>.Ok(ComputeFreeSlots(query, busy, zone));
    }

    /// <summary>Saf hesap: her gün için pencereden meşgul aralıkları çıkarır, en az süreyi karşılayan boşlukları döner.</summary>
    public static List<FreeSlotDto> ComputeFreeSlots(
        FreeTimeQueryDto query, IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> busy, TimeZoneInfo zone)
    {
        var sorted = busy.OrderBy(b => b.Start).ToList();
        var slots = new List<FreeSlotDto>();
        var minimum = TimeSpan.FromMinutes(query.MinMinutes);

        for (var day = query.From; day <= query.To; day = day.AddDays(1))
        {
            var windowStart = RecurrenceExpander.LocalToUtc(day, query.WindowStart, zone);
            var windowEnd = RecurrenceExpander.LocalToUtc(day, query.WindowEnd, zone);
            var cursor = windowStart;

            foreach (var (start, end) in sorted.Where(b => b.End > windowStart && b.Start < windowEnd))
            {
                if (start > cursor)
                    Add(cursor, start);
                if (end > cursor)
                    cursor = end;
            }

            if (cursor < windowEnd)
                Add(cursor, windowEnd);

            void Add(DateTimeOffset from, DateTimeOffset to)
            {
                if (to - from >= minimum)
                    slots.Add(new FreeSlotDto(
                        TimeZoneInfo.ConvertTime(from, zone), TimeZoneInfo.ConvertTime(to, zone), (int)(to - from).TotalMinutes));
            }
        }

        return slots;
    }

    private async Task<List<Activity>> LoadAsync(DateTimeOffset fromUtc, DateTimeOffset toUtcExclusive, CancellationToken cancellationToken)
    {
        var result = new List<Activity>();
        var filter = new ActivityFilterDto
        {
            From = fromUtc,
            To = toUtcExclusive.AddTicks(-1),
            PageSize = PageRequest.MaxPageSize
        };

        while (true)
        {
            var page = await _repository.GetPagedAsync(filter, _currentUser.UserId, cancellationToken);
            result.AddRange(page.Items);
            if (!page.HasNext)
                return result;
            filter.Page++;
        }
    }

    private async Task<TimeZoneInfo> GetZoneAsync(CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        return user is not null && TimeZoneInfo.TryFindSystemTimeZoneById(user.TimeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;
    }

    private static ActivityListDto ToListDto(Activity a, TimeZoneInfo zone) => new(
        a.Id, a.CategoryId, a.Title, a.Status.ToString(), a.Priority.ToString(),
        TimeZoneInfo.ConvertTime(a.Start, zone), TimeZoneInfo.ConvertTime(a.End, zone), a.IsAllDay, a.SeriesId);
}
