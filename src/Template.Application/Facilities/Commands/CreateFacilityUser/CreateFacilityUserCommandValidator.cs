using FluentValidation;
using Template.Domain.Constants;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandValidator : AbstractValidator<CreateFacilityUserCommand> {
    public CreateFacilityUserCommandValidator() {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Roles.FacilityRoles.Contains(role))
            .WithMessage("Role must be a valid facility role");
    }
}
