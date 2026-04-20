using FluentValidation;

namespace Template.Application.Facilities.Commands.RemoveFacilityUser;

public sealed class RemoveFacilityUserCommandValidator : AbstractValidator<RemoveFacilityUserCommand>
{
    public RemoveFacilityUserCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
