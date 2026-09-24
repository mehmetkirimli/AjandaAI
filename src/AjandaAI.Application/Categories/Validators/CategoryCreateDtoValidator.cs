// CategoryCreateDto girdi doğrulamasıdır.
// Name zorunlu, en fazla 100 karakter ve benzersiz olmalıdır.

using AjandaAI.Application.Categories.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Categories.Validators;

public class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateDtoValidator(ICategoryRepository repository)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.")
            .MustAsync(async (name, ct) => !await repository.NameExistsAsync(name, null, ct))
            .WithMessage("Bu isimde bir kategori zaten var.");
    }
}
