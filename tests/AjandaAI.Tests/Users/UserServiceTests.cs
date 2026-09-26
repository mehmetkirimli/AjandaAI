// UserService okuma ve yazma senaryolarının birim testleridir.
// Repository bellek içi bir fake ile değiştirilir; validator'lar gerçek sınıflardır.

using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Application.Users.Validators;
using AjandaAI.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AjandaAI.Tests.Users;

public class UserServiceTests
{
    private const string ValidTz = "UTC";

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; }

        public FakeUserRepository(params User[] items) => Items = items.ToList();

        public Task<PagedResult<User>> GetPagedAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
        {
            var all = Items.Where(u => u.IsActive).ToList();
            return Task.FromResult(new PagedResult<User>(all.Skip(filter.Skip).Take(filter.PageSize).ToList(), all.Count, filter.Page, filter.PageSize));
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(u => u.Id == id && u.IsActive));

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) && u.Id != excludeId));

        // Validator'ı geçip DB unique index'ine takılan eşzamanlı isteği taklit eder.
        public bool ThrowDuplicateOnAdd { get; set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (ThrowDuplicateOnAdd)
                throw new DuplicateEmailException(new InvalidOperationException("23505"));

            user.Id = Items.Count == 0 ? 1 : Items.Max(u => u.Id) + 1;
            Items.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static (UserService Service, FakeUserRepository Repo) Create()
    {
        var repo = new FakeUserRepository(
            new User { Id = 1, Email = "ali@example.com", DisplayName = "Ali", TimeZoneId = ValidTz },
            new User { Id = 2, Email = "ayse@example.com", DisplayName = "Ayşe", TimeZoneId = ValidTz },
            new User { Id = 3, Email = "eski@example.com", DisplayName = "Eski", TimeZoneId = ValidTz, IsActive = false });
        var service = new UserService(repo, new UserCreateDtoValidator(repo), new UserUpdateDtoValidator(repo), NullLogger<UserService>.Instance);
        return (service, repo);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveUsers()
    {
        var result = (await Create().Service.GetAllAsync(new UserFilterDto())).Data!.Items;

        Assert.Equal(new[] { 1, 2 }, result.Select(u => u.Id));
    }

    [Fact]
    public async Task GetByIdAsync_InactiveUser_IsReturned()
    {
        var result = (await Create().Service.GetByIdAsync(3)).Data;

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await Create().Service.GetByIdAsync(99)).ResultType);
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsUser()
    {
        var (service, repo) = Create();

        var response = await service.CreateAsync(new UserCreateDto("veli@example.com", "Veli", ValidTz));

        Assert.True(response.Success);
        Assert.Equal(4, response.Data!.Id);
        Assert.True(response.Data.IsActive);
        Assert.Equal(4, repo.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ReturnsFail()
    {
        var response = await Create().Service.CreateAsync(new UserCreateDto("ALI@example.com", "Ali 2", ValidTz));

        Assert.False(response.Success);
        Assert.Contains("Bu email ile kayıtlı bir kullanıcı zaten var.", response.Errors);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrowsDuplicateEmail_ReturnsConflict()
    {
        var (service, repo) = Create();
        repo.ThrowDuplicateOnAdd = true;

        var response = await service.CreateAsync(new UserCreateDto("yaris@example.com", "Yarış", ValidTz));

        Assert.False(response.Success);
        Assert.Equal(ResultType.Conflict, response.ResultType);
        Assert.Equal("Bu e-posta adresi zaten kullanılıyor.", response.Message);
        Assert.Equal(3, repo.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_InvalidFields_ReturnsAllErrors()
    {
        var response = await Create().Service.CreateAsync(new UserCreateDto("gecersiz", "", "Mars/Olympus"));

        Assert.False(response.Success);
        Assert.Equal(3, response.Errors.Count);
    }

    [Fact]
    public async Task UpdateAsync_OwnEmail_Succeeds()
    {
        var response = await Create().Service.UpdateAsync(1, new UserUpdateDto("ali@example.com", "Ali Yeni", ValidTz));

        Assert.True(response!.Success);
        Assert.Equal("Ali Yeni", response.Data!.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_OtherUsersEmail_ReturnsFail()
    {
        var response = await Create().Service.UpdateAsync(1, new UserUpdateDto("ayse@example.com", "Ali", ValidTz));

        Assert.False(response!.Success);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await Create().Service.UpdateAsync(99, new UserUpdateDto("x@example.com", "X", ValidTz))).ResultType);
    }

    [Fact]
    public async Task DeleteAsync_Existing_SetsIsActiveFalse_DoesNotRemove()
    {
        var (service, repo) = Create();

        var response = await service.DeleteAsync(1);

        Assert.True(response!.Success);
        Assert.False(repo.Items.Single(u => u.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ReturnsNull()
    {
        Assert.Equal(ResultType.NotFound, (await Create().Service.DeleteAsync(99)).ResultType);
    }
}
