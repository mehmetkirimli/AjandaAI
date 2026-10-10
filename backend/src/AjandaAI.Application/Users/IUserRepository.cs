// User kaynağına okuma/yazma erişim sözleşmesidir.
// Kullanıcı yönetimi /api/admin/users (AdminUserService) üzerindendir; liste sorgusu IAdminRepository'dedir.
// Silme metodu yoktur: kullanıcı pasife alınır (IsActive = false, UpdateAsync ile).
// Implementasyonu Infrastructure/Repositories/UserRepository.cs içindedir.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
