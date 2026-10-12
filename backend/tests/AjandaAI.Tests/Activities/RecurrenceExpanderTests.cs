// RecurrenceExpander (tekrar kuralından UTC oluşum listesi) birim testleridir; saf sınıf, sahte bağımlılık yok.
// Senaryolar: SERIES-01, 02, 03, 04, 06, 07, 08 (docs/seri-test-senaryolari.md, ADR 0022).
// Ek: Weekly başlangıç kayması, tüm gün kalıbı, yaz saati boşluğu ve belirsiz saat.

using AjandaAI.Application.Activities;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Tests.Activities;

public class RecurrenceExpanderTests
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static DateTimeOffset At(TimeZoneInfo zone, int y, int m, int d, int h, int min = 0)
    {
        var local = new DateTime(y, m, d, h, min, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTime Local(DateTimeOffset utc, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(utc, zone).DateTime;

    private static RecurrenceExpansion Run(
        TimeZoneInfo zone, DateTimeOffset start, DateTimeOffset end, RecurrenceFrequency frequency,
        DateOnly until, int mask = 0, bool allDay = false) =>
        RecurrenceExpander.Expand(start, end, allDay, zone, frequency, mask, until);

    // SERIES-01: Daily, 12 Eki 2026 21:00-21:10 -> 12 Nis 2027, Europe/Istanbul => 183 aktivite
    [Fact]
    public void SERIES_01_Daily_six_months_gives_183_occurrences_at_local_2100()
    {
        var start = At(Istanbul, 2026, 10, 12, 21);
        var result = Run(Istanbul, start, start.AddMinutes(10), RecurrenceFrequency.Daily, new DateOnly(2027, 4, 12));

        Assert.False(result.TooMany);
        Assert.Equal(183, result.Occurrences.Count);
        Assert.All(result.Occurrences, o =>
        {
            Assert.Equal(new TimeSpan(21, 0, 0), Local(o.Start, Istanbul).TimeOfDay);
            Assert.Equal(TimeSpan.FromMinutes(10), o.End - o.Start);
        });
        Assert.Equal(new DateTime(2027, 4, 12, 21, 0, 0), Local(result.Occurrences[^1].Start, Istanbul));
    }

    // SERIES-02: Weekdays, 1 hafta (Pzt 12 Eki - Paz 18 Eki) => yalnızca Pzt-Cum
    [Fact]
    public void SERIES_02_Weekdays_one_week_gives_five_weekdays()
    {
        var start = At(Istanbul, 2026, 10, 12, 9);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Weekdays, new DateOnly(2026, 10, 18));

        Assert.Equal(5, result.Occurrences.Count);
        Assert.All(result.Occurrences, o => Assert.DoesNotContain(Local(o.Start, Istanbul).DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }));
    }

    // SERIES-03: Weekends, 2 hafta => 4 aktivite, yalnızca Cmt-Paz
    [Fact]
    public void SERIES_03_Weekends_two_weeks_gives_four_weekend_days()
    {
        var start = At(Istanbul, 2026, 10, 12, 9);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Weekends, new DateOnly(2026, 10, 25));

        Assert.Equal(4, result.Occurrences.Count);
        Assert.All(result.Occurrences, o => Assert.Contains(Local(o.Start, Istanbul).DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }));
    }

    // SERIES-04: Weekly [Pzt, Çar, Cum], 2 hafta => 6 aktivite, yalnızca o günler
    [Fact]
    public void SERIES_04_Weekly_mon_wed_fri_two_weeks_gives_six()
    {
        var start = At(Istanbul, 2026, 10, 12, 18);
        var mask = RecurrenceExpander.ToMask(new[] { 1, 3, 5 });
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 25), mask);

        Assert.Equal(6, result.Occurrences.Count);
        Assert.All(result.Occurrences, o => Assert.Contains(Local(o.Start, Istanbul).DayOfWeek,
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }));
    }

    [Fact]
    public void Weekly_when_start_day_not_selected_first_occurrence_is_next_selected_day()
    {
        // 13 Eki 2026 Salı; yalnızca Cuma seçili -> ilk oluşum 16 Eki.
        var start = At(Istanbul, 2026, 10, 13, 18);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 31),
            RecurrenceExpander.ToMask(new[] { 5 }));

        Assert.Equal(new DateTime(2026, 10, 16, 18, 0, 0), Local(result.Occurrences[0].Start, Istanbul));
        Assert.Equal(3, result.Occurrences.Count);
    }

    // SERIES-06: Monthly, 31 Ocak başlangıç, 4 ay => 31 Oca, 28 Şub, 31 Mar, 30 Nis
    [Fact]
    public void SERIES_06_Monthly_from_31st_uses_last_day_of_short_months()
    {
        var start = At(Istanbul, 2027, 1, 31, 10);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Monthly, new DateOnly(2027, 4, 30));

        var days = result.Occurrences.Select(o => DateOnly.FromDateTime(Local(o.Start, Istanbul))).ToList();
        Assert.Equal(new[] { new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31), new DateOnly(2027, 4, 30) }, days);
    }

    // SERIES-07: Europe/Berlin, Daily 21:00, Mart sonu (28 Mart 2027 yaz saati) => yerel 21:00 kalır, UTC 20:00 -> 19:00
    [Fact]
    public void SERIES_07_Dst_transition_keeps_local_2100_and_shifts_utc()
    {
        var start = At(Berlin, 2027, 3, 25, 21);
        var result = Run(Berlin, start, start.AddMinutes(30), RecurrenceFrequency.Daily, new DateOnly(2027, 3, 31));

        Assert.Equal(7, result.Occurrences.Count);
        Assert.All(result.Occurrences, o => Assert.Equal(new TimeSpan(21, 0, 0), Local(o.Start, Berlin).TimeOfDay));
        // 25, 26, 27 Mart: kış (UTC+1) -> 20:00 UTC; 28 Mart ve sonrası: yaz (UTC+2) -> 19:00 UTC.
        Assert.Equal(new[] { 20, 20, 20, 19, 19, 19, 19 }, result.Occurrences.Select(o => o.Start.UtcDateTime.Hour).ToArray());
    }

    // SERIES-08: 367 oluşum üretecek kural => TooMany, üretim 366'da durur (tüm dizi üretilmez)
    [Fact]
    public void SERIES_08_More_than_366_occurrences_sets_TooMany_and_stops()
    {
        var start = At(Istanbul, 2026, 1, 1, 10);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Daily, new DateOnly(2027, 1, 2));

        Assert.True(result.TooMany);
        Assert.Equal(RecurrenceExpander.MaxOccurrences, result.Occurrences.Count);
    }

    [Fact]
    public void Exactly_366_occurrences_is_allowed()
    {
        var start = At(Istanbul, 2028, 1, 1, 10);
        var result = Run(Istanbul, start, start.AddHours(1), RecurrenceFrequency.Daily, new DateOnly(2028, 12, 31));

        Assert.False(result.TooMany);
        Assert.Equal(366, result.Occurrences.Count);
    }

    [Fact]
    public void Dst_gap_local_time_shifts_forward_one_hour()
    {
        // Berlin 28 Mart 2027 02:30 yerel saati yoktur (02:00 -> 03:00); 03:30 olmalı.
        var start = At(Berlin, 2027, 3, 27, 2, 30);
        var result = Run(Berlin, start, start.AddHours(1), RecurrenceFrequency.Daily, new DateOnly(2027, 3, 28));

        Assert.Equal(new DateTime(2027, 3, 27, 2, 30, 0), Local(result.Occurrences[0].Start, Berlin));
        Assert.Equal(new DateTime(2027, 3, 28, 3, 30, 0), Local(result.Occurrences[1].Start, Berlin));
    }

    [Fact]
    public void Dst_ambiguous_local_time_picks_standard_offset()
    {
        // Berlin 31 Ekim 2027 02:30 iki kez yaşanır; standart (UTC+1) -> 01:30 UTC.
        var start = At(Berlin, 2027, 10, 30, 2, 30);
        var result = Run(Berlin, start, start.AddHours(1), RecurrenceFrequency.Daily, new DateOnly(2027, 10, 31));

        Assert.Equal(new DateTime(2027, 10, 31, 1, 30, 0), result.Occurrences[1].Start.UtcDateTime);
    }

    [Fact]
    public void AllDay_uses_local_day_0000_to_2359()
    {
        var start = At(Istanbul, 2026, 10, 12, 0);
        var end = At(Istanbul, 2026, 10, 12, 23, 59);
        var result = Run(Istanbul, start, end, RecurrenceFrequency.Daily, new DateOnly(2026, 10, 13), allDay: true);

        Assert.Equal(2, result.Occurrences.Count);
        Assert.All(result.Occurrences, o =>
        {
            Assert.Equal(TimeSpan.Zero, Local(o.Start, Istanbul).TimeOfDay);
            Assert.Equal(new TimeSpan(23, 59, 0), Local(o.End, Istanbul).TimeOfDay);
        });
    }

    [Fact]
    public void Mask_round_trips_turkish_day_numbers()
    {
        Assert.Equal(1 | 4 | 16, RecurrenceExpander.ToMask(new[] { 1, 3, 5 }));
        Assert.Equal(64, RecurrenceExpander.ToMask(new[] { 7 }));
        Assert.Equal(new[] { 1, 3, 5 }, RecurrenceExpander.FromMask(21));
    }
}
