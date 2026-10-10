// Oturumdaki kullanıcının bilgisini döner (GET /api/auth/me, ADR 0021).
// AuthService'ten ayrıdır: AuthService IEmailSender'a bağlıdır ve o Production'da henüz kayıtlı
// değildir; /me e-posta altyapısını beklememelidir.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;

namespace AjandaAI.Application.Auth;

public class MeService
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;

    public MeService(ICurrentUser currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<ApiResponse<MeDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        // Pasif kullanıcının token'ı en fazla 15 dk yaşar (ADR 0018); bu sürede de bilgisi dönmez.
        return user is null || !user.IsActive
            ? ApiResponse<MeDto>.Unauthorized(AuthService.InvalidSessionMessage)
            : ApiResponse<MeDto>.Ok(new MeDto(user.Id, user.Email, user.DisplayName, user.TimeZoneId, user.Role));
    }
}
