// ActivityCreateDto için FluentValidation kurallarıdır.
// İlişkisel alanlar (CategoryId, UserId) repository ile MustAsync üzerinden doğrulanır.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Users;
using AjandaAI.Domain.Enums;
using FluentValidation;

namespace AjandaAI.Application.Activities.Validators;

public class ActivityCreateDtoValidator : AbstractValidator<ActivityCreateDto>
{
    private readonly ICategoryRepository _categories;

    private readonly IUserRepository _users;

    public ActivityCreateDtoValidator(ICategoryRepository categories, IUserRepository users)
    {
        _categories = categories;
        _users = users;

        RuleFor(x => x.UserId)
            .MustAsync((id, ct) => _users.IsActiveAsync(id, ct))
            .WithMessage("Kullanıcı bulunamadı veya pasif.");

        RuleFor(x => x.CategoryId)
            .MustAsync(CategoryIsActiveAsync)
            .WithMessage("Kategori bulunamadı veya aktif değil.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz durum.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Geçersiz öncelik.");
        RuleFor(x => x.EnergyLevel).IsInEnum().WithMessage("Geçersiz enerji seviyesi.");

        RuleFor(x => x.End)
            .GreaterThan(x => x.Start).WithMessage("Bitiş zamanı başlangıçtan sonra olmalıdır.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 10).When(x => x.Rating.HasValue)
            .WithMessage("Puan 1 ile 10 arasında olmalıdır.");

        RuleFor(x => x.Rating)
            .Null().When(x => x.Status != ActivityStatus.Completed)
            .WithMessage("Puan yalnızca tamamlanan aktivitelere verilebilir.");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThanOrEqualTo(0).WithMessage("Tahmini bütçe negatif olamaz.");
    }

    private async Task<bool> CategoryIsActiveAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(categoryId, cancellationToken);
        return category is { IsActive: true };
    }
}
