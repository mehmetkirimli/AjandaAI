// Asistan araçlarının kayıt defteri (ADR 0025 §3). Araçlar ince adaptördür: web ve MCP'nin kullandığı Application
// servislerini çağırır. Kimlik her zaman sunucudaki oturumdur (ICurrentUser); modelin girdisindeki userId vb. alanlar
// şemada yoktur ve yok sayılır (ASSIST-13). Yalnızca AssistantToolOptions'ta etkin olanlar üretilir (ASSIST-14).
// Öneri araçları YAZMAZ: oluşturmayla aynı doğrulamadan geçen girdiyi AssistantTurn.Proposals'a ekler (ASSIST-40/41).

using System.ComponentModel;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Assistant.Dtos;
using AjandaAI.Application.Assistant.External;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;
using Microsoft.Extensions.AI;

namespace AjandaAI.Application.Assistant;

/// <summary>Modele dönen araç hatası; Türkçe metin modelin kullanıcıya açıklaması içindir.</summary>
public record AssistantToolError(string Hata);

public class AssistantToolRegistry
{
    private readonly ActivityService _activities;
    private readonly AgendaService _agenda;
    private readonly CategoryService _categories;
    private readonly IWeatherClient _weather;
    private readonly IPlaceSearchClient _places;
    private readonly IHolidayClient _holidays;
    private readonly AssistantToolOptions _options;

    public AssistantToolRegistry(ActivityService activities, AgendaService agenda, CategoryService categories,
        IWeatherClient weather, IPlaceSearchClient places, IHolidayClient holidays, AssistantToolOptions options)
    {
        _activities = activities;
        _agenda = agenda;
        _categories = categories;
        _weather = weather;
        _places = places;
        _holidays = holidays;
        _options = options;
    }

    /// <summary>Bu istek için etkin araçlar. Kapalı araç listede yoktur; model çağırırsa "bulunamadı" olarak reddedilir.</summary>
    public IList<AITool> Build(AssistantTurn turn) =>
        Create(turn).Where(f => _options.IsEnabled(f.Name)).Cast<AITool>().ToList();

    private IEnumerable<AIFunction> Create(AssistantTurn turn)
    {
        yield return AIFunctionFactory.Create(
            async ([Description("Haftanın içindeki herhangi bir gün (yyyy-MM-dd)")] DateOnly tarih, CancellationToken ct) =>
                Unwrap(await _agenda.GetWeekAsync(tarih, ct)),
            AssistantToolNames.HaftayiListele,
            "Verilen tarihin haftasındaki (Pazartesi-Pazar, kullanıcının saat diliminde) etkin aktiviteleri listeler.");

        yield return AIFunctionFactory.Create(
            async ([Description("Aralığın ilk günü (yyyy-MM-dd)")] DateOnly baslangicGunu,
                   [Description("Aralığın son günü (yyyy-MM-dd, dahil)")] DateOnly bitisGunu,
                   [Description("İstenen en az boşluk, dakika")] int enAzDakika = 30,
                   [Description("Gün içi pencere başlangıcı (HH:mm)")] string? gunBaslangici = null,
                   [Description("Gün içi pencere bitişi (HH:mm)")] string? gunBitisi = null,
                   CancellationToken ct = default) =>
            {
                if (!TimeOnly.TryParse(gunBaslangici ?? "08:00", out var windowStart) ||
                    !TimeOnly.TryParse(gunBitisi ?? "22:00", out var windowEnd))
                    return new AssistantToolError("Gün içi pencere saatleri HH:mm biçiminde olmalıdır.");
                var query = new FreeTimeQueryDto(baslangicGunu, bitisGunu, enAzDakika <= 0 ? 30 : enAzDakika, windowStart, windowEnd);
                return Unwrap(await _agenda.FindFreeTimeAsync(query, ct));
            },
            AssistantToolNames.BosZamanBul,
            "Tarih aralığında (en fazla 31 gün), gün içi pencerede en az verilen süre kadar boş zamanları bulur. " +
            "Tüm gün süren ve iptal edilen aktiviteler meşgul sayılmaz.");

        yield return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
                Unwrap(await _categories.GetAllAsync(new CategoryFilterDto { PageSize = PageRequest.MaxPageSize }, ct),
                    page => page.Items),
            AssistantToolNames.KategorileriListele,
            "Etkin kategorileri (kimlik ve ad) listeler. Öneri araçları için kategori kimliğini buradan bul.");

        yield return AIFunctionFactory.Create(
            async ([Description("Yıl (örn. 2026)")] int yil, CancellationToken ct) =>
                await External(ct, "Tatil servisine şu an ulaşılamıyor; tatilleri kontrol edemedim.",
                    async () => (object)await _holidays.GetHolidaysAsync(yil, ct)),
            AssistantToolNames.TatilleriListele,
            "Türkiye'nin verilen yıldaki resmî tatillerini listeler (Nager.Date). Dinî bayramlar listede olmayabilir; " +
            "yoksa kullanıcıya bunu söyle.");

        yield return AIFunctionFactory.Create(
            async ([Description("Enlem (yer_ara ile bul)")] double enlem,
                   [Description("Boylam (yer_ara ile bul)")] double boylam,
                   [Description("İlk gün (yyyy-MM-dd)")] DateOnly baslangicGunu,
                   [Description("Son gün (yyyy-MM-dd, dahil; en fazla 16 gün ileri)")] DateOnly bitisGunu,
                   CancellationToken ct = default) =>
                await External(ct, "Hava durumu servisine şu an ulaşılamıyor; hava durumunu kontrol edemedim.",
                    async () => (object)await _weather.GetDailyForecastAsync(enlem, boylam, baslangicGunu, bitisGunu, ct)),
            AssistantToolNames.HavaDurumu,
            "Konumun günlük hava tahminini döner (Open-Meteo): en yüksek/en düşük sıcaklık, yağış olasılığı, özet.");

        yield return AIFunctionFactory.Create(
            async ([Description("Aranacak yer (örn. 'Belgrad Ormanı, İstanbul')")] string sorgu, CancellationToken ct) =>
                string.IsNullOrWhiteSpace(sorgu)
                    ? new AssistantToolError("Aranacak yer boş olamaz.")
                    : await External(ct, "Yer arama servisine şu an ulaşılamıyor.",
                        async () => (object)await _places.SearchAsync(sorgu, ct)),
            AssistantToolNames.YerAra,
            "Yer adını koordinata çevirir (OpenStreetMap). Rota veya yol süresi vermez.");

        yield return AIFunctionFactory.Create(
            async ([Description("Aktivitenin başlığı")] string baslik,
                   [Description("Başlangıç, ISO 8601 ve offset ile (örn. 2026-11-14T10:00:00+03:00)")] DateTimeOffset baslangic,
                   [Description("Bitiş, ISO 8601 ve offset ile; başlangıçtan sonra olmalı")] DateTimeOffset bitis,
                   [Description("Kategori kimliği (kategorileri_listele)")] int kategoriId,
                   [Description("İsteğe bağlı açıklama")] string? aciklama = null,
                   [Description("İsteğe bağlı konum")] string? konum = null,
                   [Description("Tüm gün süren aktivite mi")] bool tumGun = false,
                   CancellationToken ct = default) =>
            {
                var dto = new ActivityCreateDto(
                    kategoriId, baslik, aciklama ?? string.Empty, ActivityStatus.Planned, Priority.Medium, EnergyLevel.Medium,
                    baslangic, bitis, tumGun, konum, IsFlexible: false, EstimatedBudget: 0, Rating: null, WouldRepeat: null);
                var errors = await _activities.ValidateCreateAsync(dto, ct);
                if (errors.Count > 0)
                    return ValidationError(errors);
                turn.Proposals.Add(new AssistantProposalDto(AssistantProposalDto.ActivityKind, dto, null));
                return (object)new { durum = "Öneri kullanıcıya onay kartı olarak gösterildi; kayıt kullanıcı onaylayınca oluşur." };
            },
            AssistantToolNames.AktiviteOner,
            "Tek bir aktiviteyi kullanıcının onayına sunar (veritabanına YAZMAZ). Başlangıç en fazla 30 gün geçmişte olabilir. " +
            "Tekrarlayan işler için seri_oner kullan.");

        yield return AIFunctionFactory.Create(
            async ([Description("Aktivitenin başlığı")] string baslik,
                   [Description("İlk oluşumun başlangıcı, ISO 8601 ve offset ile")] DateTimeOffset ilkBaslangic,
                   [Description("İlk oluşumun bitişi, ISO 8601 ve offset ile")] DateTimeOffset ilkBitis,
                   [Description("Kategori kimliği (kategorileri_listele)")] int kategoriId,
                   [Description("Sıklık: Daily, Weekdays, Weekends, Weekly (seçilen günler), Monthly")] string siklik,
                   [Description("Serinin son günü (yyyy-MM-dd, dahil)")] DateOnly sonGun,
                   [Description("Yalnızca Weekly için günler: 1=Pzt ... 7=Paz")] List<int>? gunler = null,
                   [Description("İsteğe bağlı açıklama")] string? aciklama = null,
                   [Description("Her oluşum için kaç dakika önce hatırlatılsın (isteğe bağlı)")] int? hatirlatmaDakikaOnce = null,
                   CancellationToken ct = default) =>
            {
                if (!Enum.TryParse<RecurrenceFrequency>(siklik, ignoreCase: true, out var frequency) ||
                    !Enum.IsDefined(frequency))
                    return new AssistantToolError("Geçersiz tekrar sıklığı. Daily, Weekdays, Weekends, Weekly veya Monthly olmalı.");
                var dto = new ActivitySeriesCreateDto(
                    kategoriId, baslik, aciklama ?? string.Empty, ActivityStatus.Planned, Priority.Medium, EnergyLevel.Medium,
                    ilkBaslangic, ilkBitis, IsAllDay: false, Location: null, IsFlexible: false, EstimatedBudget: 0,
                    new RecurrenceDto(frequency, gunler, sonGun), hatirlatmaDakikaOnce);
                var errors = await _activities.ValidateSeriesAsync(dto, ct);
                if (errors.Count > 0)
                    return ValidationError(errors);
                turn.Proposals.Add(new AssistantProposalDto(AssistantProposalDto.SeriesKind, null, dto));
                return (object)new { durum = "Seri önerisi kullanıcıya onay kartı olarak gösterildi; kayıt kullanıcı onaylayınca oluşur." };
            },
            AssistantToolNames.SeriOner,
            "Tekrarlayan bir aktivite serisini kullanıcının onayına sunar (veritabanına YAZMAZ, en fazla 366 oluşum). " +
            "Başlangıç/bitiş İLK oluşumun zamanıdır.");
    }

    private static object ValidationError(List<string> errors) =>
        new AssistantToolError("Öneri geçersiz: " + string.Join(" ", errors));

    private static object Unwrap<T>(ApiResponse<T> response) => Unwrap(response, d => d!);

    private static object Unwrap<T, TOut>(ApiResponse<T> response, Func<T, TOut> select)
    {
        if (response.Success && response.Data is not null)
            return select(response.Data)!;
        var details = response.Errors.Count > 0 ? ": " + string.Join(" ", response.Errors) : string.Empty;
        return new AssistantToolError(response.Message + details);
    }

    // Dış servis hatası sohbeti düşürmez; modele Türkçe hata döner (ASSIST-15, 17).
    private static async Task<object> External(CancellationToken ct, string message, Func<Task<object>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return new AssistantToolError(message);
        }
    }
}
