// Admin'in aktivite ve hatırlatma üstverisi okuması ile aktivite moderasyonu (ADR 0018 Karar 6).
// İçerik (Title, Description, Location, Note) okunmaz; kullanıcı adına düzenleme bu sürümde YOK.
// Yazma işlemleri {AdminId} ile loglanır: audit log gelene kadar "kim yaptı" kaydı loglardır.

using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Common;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Admin;

public class AdminContentService
{
    private readonly ICurrentUser _currentUser;
    private readonly IAdminRepository _admin;
    private readonly ILogger<AdminContentService> _logger;

    public AdminContentService(ICurrentUser currentUser, IAdminRepository admin, ILogger<AdminContentService> logger)
    {
        _currentUser = currentUser;
        _admin = admin;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<AdminActivityDto>>> GetActivitiesAsync(AdminActivityFilterDto filter, CancellationToken cancellationToken = default) =>
        ApiResponse<PagedResult<AdminActivityDto>>.Ok(await _admin.GetActivitiesPagedAsync(filter, cancellationToken));

    public async Task<ApiResponse<AdminActivityDto>> GetActivityAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _admin.GetActivityAsync(id, cancellationToken);
        return activity is null
            ? ApiResponse<AdminActivityDto>.NotFound(ActivityNotFound(id))
            : ApiResponse<AdminActivityDto>.Ok(activity);
    }

    public async Task<ApiResponse<PagedResult<AdminReminderDto>>> GetRemindersAsync(AdminReminderFilterDto filter, CancellationToken cancellationToken = default) =>
        ApiResponse<PagedResult<AdminReminderDto>>.Ok(await _admin.GetRemindersPagedAsync(filter, cancellationToken));

    public async Task<ApiResponse<AdminActivityDto>> DeactivateActivityAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _admin.DeactivateActivityAsync(id, DateTimeOffset.UtcNow, cancellationToken))
            return ApiResponse<AdminActivityDto>.NotFound(ActivityNotFound(id));

        _logger.LogInformation("Admin aktiviteyi pasife aldı {AdminId} {ActivityId}", _currentUser.UserId, id);
        return ApiResponse<AdminActivityDto>.Ok((await _admin.GetActivityAsync(id, cancellationToken))!, "Aktivite pasife alındı.");
    }

    private static string ActivityNotFound(int id) => $"Activity {id} bulunamadı.";
}
