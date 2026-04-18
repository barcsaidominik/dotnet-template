using FluentValidation;

namespace Template.Application.Admin.Commands.CreateFacility;

public sealed class CreateFacilityCommandValidator : AbstractValidator<CreateFacilityCommand>
{
    public CreateFacilityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
