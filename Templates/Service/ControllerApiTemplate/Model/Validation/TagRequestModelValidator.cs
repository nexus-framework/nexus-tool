using FluentValidation;
using {{ROOT_NAMESPACE}}.Data.Repositories;

namespace {{ROOT_NAMESPACE}}.Model.Validation;

[ExcludeFromCodeCoverage]
public class TagRequestModelValidator : AbstractValidator<TagRequestModel>
{
    public TagRequestModelValidator(TagRepository repository)
    {
        RuleFor(c => c.Name)
            .NotNull()
            .MaximumLength(20)
            .MustAsync(async (name, cancellationToken) =>
            {
                bool exists = await repository.ExistsWithNameAsync(name, cancellationToken);
                return !exists;
            })
            .WithMessage("Tag with this name already exists");
    }
}