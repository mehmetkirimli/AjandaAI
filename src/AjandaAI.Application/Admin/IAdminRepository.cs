// Admin uçlarının SAHİPLİK KOŞULSUZ okuma sorguları. Yalnızca /api/admin/... servisleri kullanır;
// kullanıcı endpoint'lerinde admin istisnası yoktur (ADR 0018 Karar 3, 6).
// Aktivite/hatırlatma sorguları sonucu doğrudan üstveri DTO'suna projekte eder: içerik kolonları
// (title, description, location, note) veritabanından hiç okunmaz.
// Implementasyonu Infrastructure/Repositories/AdminRepository.cs içindedir.

using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Common;

namespace AjandaAI.Application.Admin;

public interface IAdminRepository
{
    Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(AdminUserFilterDto filter, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminActivityDto>> GetActivitiesPagedAsync(AdminActivityFilterDto filter, CancellationToken cancellationToken = default);

    Task<AdminActivityDto?> GetActivityAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminReminderDto>> GetRemindersPagedAsync(AdminReminderFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>Aktiviteyi pasife alır (moderasyon). Kayıt yoksa false.</summary>
    Task<bool> DeactivateActivityAsync(int id, DateTimeOffset now, CancellationToken cancellationToken = default);
}
