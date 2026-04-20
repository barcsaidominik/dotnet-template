using FluentValidation;

namespace Template.Application.Admin.Commands.DeleteFacility;

public sealed class DeleteFacilityCommandValidator : AbstractValidator<DeleteFacilityCommand>
{
    public DeleteFacilityCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
    }
}
