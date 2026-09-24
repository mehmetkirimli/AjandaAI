// ActivityUpdateDto için FluentValidation kurallarıdır.
// İlişkisel alanlar (CategoryId) repository ile MustAsync üzerinden doğrulanır.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Categories;
using AjandaAI.Domain.Enums;
using FluentValidation;

namespace AjandaAI.Application.Activities.Validators;

public class ActivityUpdateDtoValidator : AbstractValidator<ActivityUpdateDto>
{
    private readonly ICategoryRepository _categories;

    public ActivityUpdateDtoValidator(ICategoryRepository categories)
    {
        _categories = categories;

        RuleFor(x => x.CategoryId)
            .MustAsync((id, ct) => _categories.IsActiveAsync(id, ct))
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
}
