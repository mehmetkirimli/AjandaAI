// /api/admin/... uçlarının girdi ve çıktı modelleri (ADR 0018 Karar 6).
// Aktivite ve hatırlatma için yalnızca ÜSTVERİ taşınır: Title, Description, Location, Note
// bu modellerde YOKTUR (veri minimizasyonu; seçilmeyen veri loga veya dışarı sızamaz).

using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Admin.Dtos;

public record AdminUserDto(
    int Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    UserRole Role,
    bool IsActive,
    DateTimeOffset? EmailConfirmedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Admin'in oluşturduğu kullanıcı doğrulanmış sayılır; şifre kayıttaki kurallara tabidir.</summary>
public record AdminUserCreateDto(string Email, string DisplayName, string TimeZoneId, string Password, UserRole? Role);

/// <summary>Role zorunludur: eksik gönderim sessizce User sayılıp admin düşürülmesin.</summary>
public record AdminUserUpdateDto(string Email, string DisplayName, string TimeZoneId, UserRole? Role);

/// <summary>Pasif kullanıcılar da listelenir; IsActive verilirse ona göre süzülür.</summary>
public class AdminUserFilterDto : PageRequest
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}

public record AdminActivityDto(
    int Id,
    int UserId,
    int CategoryId,
    ActivityStatus Status,
    Priority Priority,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class AdminActivityFilterDto : PageRequest
{
    public int? UserId { get; set; }

    public ActivityStatus? Status { get; set; }

    public bool? IsActive { get; set; }
}

public record AdminReminderDto(
    int Id,
    int ActivityId,
    int UserId,
    DateTimeOffset RemindAt,
    bool IsSent,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt);

public class AdminReminderFilterDto : PageRequest
{
    public int? UserId { get; set; }

    public int? ActivityId { get; set; }

    public bool? IsSent { get; set; }
}
