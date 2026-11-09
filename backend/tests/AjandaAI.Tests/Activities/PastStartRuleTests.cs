// PastStartRule sınır testleridir (ADR 0024 §2b): gün sınırı kullanıcının saat dilimine göredir.
// Uç nokta senaryoları (PAST-01..05) entegrasyon testlerindedir (IntegrationTests/Activities/PastStartRuleTests.cs).

using AjandaAI.Application.Activities;

namespace AjandaAI.Tests.Activities;

public class PastStartRuleTests
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    // PAST-01 / PAST-02 sınırı: tam 30 gün önce kabul, 31 gün önce red.
    [Theory]
    [InlineData(29, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void PAST_01_02_Boundary_is_30_days(int daysAgo, bool allowed) =>
        Assert.Equal(allowed, PastStartRule.IsAllowed(Now.AddDays(-daysAgo), Now, Istanbul));

    [Fact]
    public void Day_boundary_uses_user_time_zone()
    {
        // İstanbul'da 10 Ekim 00:30 (UTC 9 Ekim 21:30): yerel gün 30 gün öncesi -> kabul.
        var start = new DateTimeOffset(2026, 10, 9, 21, 30, 0, TimeSpan.Zero);
        Assert.True(PastStartRule.IsAllowed(start, Now, Istanbul));
        Assert.False(PastStartRule.IsAllowed(start, Now, TimeZoneInfo.Utc));
    }

    [Fact]
    public void Message_is_turkish() =>
        Assert.Equal("Başlangıç en fazla 30 gün öncesi olabilir.", PastStartRule.Message);
}
