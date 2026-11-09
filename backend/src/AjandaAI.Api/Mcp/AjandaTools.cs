// Claude'a açılan MCP araçları (ADR 0024 §2). Araçlar ince bir adaptördür: iş kuralı içermez,
// web istemcisinin kullandığı Application servislerini çağırır. Böylece doğrulama, 30 gün kuralı (§2b),
// sahiplik (ICurrentUser = anahtarın sahibi) ve loglama aynı yoldan geçer.
// Servisin başarısız yanıtı McpException'a çevrilir; Claude Türkçe hata mesajını görür, kayıt oluşmaz.
// Silme/güncelleme aracı bilinçli olarak yoktur.

using System.ComponentModel;
using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AjandaAI.Api.Mcp;

[McpServerToolType]
public sealed class AjandaTools(ActivityService activities, AgendaService agenda, CategoryService categories)
{
    [McpServerTool(Name = "aktivite_ekle", Destructive = false)]
    [Description("Kullanıcının ajandasına tek bir aktivite ekler. Tekrarlayan işler için seri_ekle kullan. " +
                 "Kategori kimliğini kategorileri_listele ile bul. Başlangıç en fazla 30 gün geçmişte olabilir.")]
    public async Task<ActivityDetailDto> AktiviteEkle(
        [Description("Aktivitenin başlığı")] string baslik,
        [Description("Başlangıç, ISO 8601 ve offset ile (örn. 2026-11-12T19:00:00+03:00)")] DateTimeOffset baslangic,
        [Description("Bitiş, ISO 8601 ve offset ile; başlangıçtan sonra olmalı")] DateTimeOffset bitis,
        [Description("Kategori kimliği (kategorileri_listele)")] int kategoriId,
        [Description("İsteğe bağlı açıklama")] string? aciklama = null,
        [Description("İsteğe bağlı konum")] string? konum = null,
        [Description("Tüm gün süren aktivite mi")] bool tumGun = false,
        CancellationToken cancellationToken = default)
    {
        var dto = new ActivityCreateDto(
            kategoriId, baslik, aciklama ?? string.Empty, ActivityStatus.Planned, Priority.Medium, EnergyLevel.Medium,
            baslangic, bitis, tumGun, konum, IsFlexible: false, EstimatedBudget: 0, Rating: null, WouldRepeat: null);
        return Unwrap(await activities.CreateAsync(dto, cancellationToken));
    }

    [McpServerTool(Name = "seri_ekle", Destructive = false)]
    [Description("Tekrarlayan bir aktivite serisini tek seferde ekler (en fazla 366 oluşum). " +
                 "Örn. 'bir yıl boyunca her gece 23:00'te 10 dakika kitap oku'. Başlangıç/bitiş İLK oluşumun zamanıdır.")]
    public async Task<ActivitySeriesCreatedDto> SeriEkle(
        [Description("Aktivitenin başlığı")] string baslik,
        [Description("İlk oluşumun başlangıcı, ISO 8601 ve offset ile")] DateTimeOffset ilkBaslangic,
        [Description("İlk oluşumun bitişi, ISO 8601 ve offset ile")] DateTimeOffset ilkBitis,
        [Description("Kategori kimliği (kategorileri_listele)")] int kategoriId,
        [Description("Sıklık: Daily (her gün), Weekdays (Pzt-Cum), Weekends (Cmt-Paz), Weekly (seçilen günler), Monthly (her ay aynı gün)")] RecurrenceFrequency siklik,
        [Description("Serinin son günü (yyyy-MM-dd, dahil)")] DateOnly sonGun,
        [Description("Yalnızca Weekly için günler: 1=Pzt ... 7=Paz")] List<int>? gunler = null,
        [Description("İsteğe bağlı açıklama")] string? aciklama = null,
        [Description("Her oluşum için kaç dakika önce hatırlatılsın (isteğe bağlı)")] int? hatirlatmaDakikaOnce = null,
        CancellationToken cancellationToken = default)
    {
        var dto = new ActivitySeriesCreateDto(
            kategoriId, baslik, aciklama ?? string.Empty, ActivityStatus.Planned, Priority.Medium, EnergyLevel.Medium,
            ilkBaslangic, ilkBitis, IsAllDay: false, Location: null, IsFlexible: false, EstimatedBudget: 0,
            new RecurrenceDto(siklik, gunler, sonGun), hatirlatmaDakikaOnce);
        return Unwrap(await activities.CreateSeriesAsync(dto, cancellationToken));
    }

    [McpServerTool(Name = "haftayi_listele", ReadOnly = true)]
    [Description("Verilen tarihin haftasındaki (Pazartesi-Pazar, kullanıcının saat diliminde) etkin aktiviteleri listeler.")]
    public async Task<AgendaWeekDto> HaftayiListele(
        [Description("Haftanın içindeki herhangi bir gün (yyyy-MM-dd)")] DateOnly tarih,
        CancellationToken cancellationToken = default) =>
        Unwrap(await agenda.GetWeekAsync(tarih, cancellationToken));

    [McpServerTool(Name = "bos_zaman_bul", ReadOnly = true)]
    [Description("Tarih aralığında, gün içi pencere içinde en az verilen süre kadar boş zamanları bulur (en fazla 31 gün). " +
                 "Tüm gün süren ve iptal edilen aktiviteler meşgul sayılmaz.")]
    public async Task<List<FreeSlotDto>> BosZamanBul(
        [Description("Aralığın ilk günü (yyyy-MM-dd)")] DateOnly baslangicGunu,
        [Description("Aralığın son günü (yyyy-MM-dd, dahil)")] DateOnly bitisGunu,
        [Description("İstenen en az boşluk, dakika")] int enAzDakika = 30,
        [Description("Gün içi pencere başlangıcı (HH:mm)")] string gunBaslangici = "08:00",
        [Description("Gün içi pencere bitişi (HH:mm)")] string gunBitisi = "22:00",
        CancellationToken cancellationToken = default)
    {
        if (!TimeOnly.TryParse(gunBaslangici, out var windowStart) || !TimeOnly.TryParse(gunBitisi, out var windowEnd))
        {
            throw new McpException("Gün içi pencere saatleri HH:mm biçiminde olmalıdır.");
        }

        var query = new FreeTimeQueryDto(baslangicGunu, bitisGunu, enAzDakika, windowStart, windowEnd);
        return Unwrap(await agenda.FindFreeTimeAsync(query, cancellationToken));
    }

    [McpServerTool(Name = "kategorileri_listele", ReadOnly = true)]
    [Description("Etkin kategorileri (kimlik ve ad) yönetimdeki sırasıyla listeler.")]
    public async Task<IReadOnlyList<CategoryListDto>> KategorileriListele(CancellationToken cancellationToken = default) =>
        Unwrap(await categories.GetAllAsync(
            new CategoryFilterDto { PageSize = PageRequest.MaxPageSize }, cancellationToken)).Items;

    private static T Unwrap<T>(ApiResponse<T> response)
    {
        if (response.Success && response.Data is not null)
        {
            return response.Data;
        }

        var details = response.Errors.Count > 0 ? ": " + string.Join(" ", response.Errors) : string.Empty;
        throw new McpException(response.Message + details);
    }
}
