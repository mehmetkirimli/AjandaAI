// CategoryUpdateDto girdi doğrulamasıdır.
// Id route'tan gelir ve RootContextData[IdKey] ile verilir;
// isim benzersizliği kaydın kendisi hariç tutularak kontrol edilir.

using AjandaAI.Application.Categories.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Categories.Validators;

public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
{
    public const string IdKey = "Id";

    public CategoryUpdateDtoValidator(ICategoryRepository repository)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.")
            .MustAsync(async (_, name, context, ct) =>
            {
                int? id = context.RootContextData.TryGetValue(IdKey, out var v) ? (int)v : null;
                return !await repository.NameExistsAsync(name, id, ct);
            })
            .WithMessage("Bu isimde bir kategori zaten var.");
    }
}
