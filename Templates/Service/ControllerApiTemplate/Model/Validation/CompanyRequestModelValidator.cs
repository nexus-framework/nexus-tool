using FluentValidation;
using {{ROOT_NAMESPACE}}.Data.Repositories;

namespace {{ROOT_NAMESPACE}}.Model.Validation;

[ExcludeFromCodeCoverage]
public class CompanyRequestModelValidator : AbstractValidator<CompanyRequestModel>
{
    public CompanyRequestModelValidator(CompanyRepository repository)
    {
        RuleFor(c => c.Name)
            .NotNull()
            .MinimumLength(5)
            .MaximumLength(255)
            .MustAsync(async (name, cancellationToken) =>
            {
                bool exists = await repository.ExistsWithNameAsync(name, cancellationToken);
                return !exists;
            })
            .WithMessage("Company with this name already exists");
    }
}