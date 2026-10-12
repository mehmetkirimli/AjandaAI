// Tekrar kuralını kullanıcının yerel saat diliminde UTC oluşum listesine çeviren saf sınıftır (ADR 0022).
// Yerel saat korunur (yaz/kış geçişinde de 21:00 kalır), süre korunur.
// Oluşum sayısı MaxOccurrences + 1'e ulaşınca üretim durur ve TooMany işaretlenir.
// DST: geçersiz yerel saat (boşluk) bir saat ileri kayar; belirsiz yerel saatte standart (küçük) offset seçilir.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Activities;

public record Occurrence(DateTimeOffset Start, DateTimeOffset End);

public record RecurrenceExpansion(IReadOnlyList<Occurrence> Occurrences, bool TooMany, DateOnly FirstLocalDate);

public static class RecurrenceExpander
{
    public const int MaxOccurrences = 366;

    /// <summary>Gün numaralarından (1=Pzt ... 7=Paz) bit maskesi üretir (Pzt=1 ... Paz=64).</summary>
    public static int ToMask(IEnumerable<int> days) => days.Aggregate(0, (mask, d) => mask | (1 << (d - 1)));

    public static List<int> FromMask(int mask) =>
        Enumerable.Range(1, 7).Where(d => (mask & (1 << (d - 1))) != 0).ToList();

    public static DateOnly LocalDateOf(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    public static RecurrenceExpansion Expand(
        DateTimeOffset start, DateTimeOffset end, bool isAllDay, TimeZoneInfo zone,
        RecurrenceFrequency frequency, int daysOfWeekMask, DateOnly until)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, zone).DateTime;
        var firstDate = DateOnly.FromDateTime(localStart);
        var timeOfDay = localStart.TimeOfDay;
        var duration = end - start;
        var result = new List<Occurrence>();

        if (frequency == RecurrenceFrequency.Weekly && daysOfWeekMask == 0)
        {
            return new RecurrenceExpansion(result, false, firstDate);
        }

        for (var date = firstDate; date <= until; date = date.AddDays(1))
        {
            if (!Matches(date, firstDate, frequency, daysOfWeekMask)) continue;

            // 367. oluşuma ulaşıldı: tüm diziyi üretmeden dur.
            if (result.Count == MaxOccurrences)
            {
                return new RecurrenceExpansion(result, true, firstDate);
            }

            if (isAllDay)
            {
                // Tek aktivite mantığıyla aynı kalıp: yerel gün 00:00 - 23:59.
                result.Add(new Occurrence(
                    ToUtc(date.ToDateTime(TimeOnly.MinValue), zone),
                    ToUtc(date.ToDateTime(new TimeOnly(23, 59)), zone)));
            }
            else
            {
                var occurrenceStart = ToUtc(date.ToDateTime(TimeOnly.FromTimeSpan(timeOfDay)), zone);
                result.Add(new Occurrence(occurrenceStart, occurrenceStart + duration));
            }
        }

        return new RecurrenceExpansion(result, false, firstDate);
    }

    private static bool Matches(DateOnly date, DateOnly first, RecurrenceFrequency frequency, int mask)
    {
        var dow = date.DayOfWeek;
        var weekend = dow is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return frequency switch
        {
            RecurrenceFrequency.Daily => true,
            RecurrenceFrequency.Weekdays => !weekend,
            RecurrenceFrequency.Weekends => weekend,
            // DayOfWeek Pazar=0; Pzt=bit0 ... Paz=bit6.
            RecurrenceFrequency.Weekly => (mask & (1 << (((int)dow + 6) % 7))) != 0,
            // Ay o güne sahip değilse ayın son günü.
            RecurrenceFrequency.Monthly => date.Day == Math.Min(first.Day, DateTime.DaysInMonth(date.Year, date.Month)),
            _ => false
        };
    }

    /// <summary>Yerel günün 00:00 anını UTC olarak döner.</summary>
    public static DateTimeOffset StartOfLocalDayUtc(DateOnly date, TimeZoneInfo zone) =>
        ToUtc(date.ToDateTime(TimeOnly.MinValue), zone);

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        var offset = zone.IsAmbiguousTime(local)
            ? zone.GetAmbiguousTimeOffsets(local).Min()
            : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
