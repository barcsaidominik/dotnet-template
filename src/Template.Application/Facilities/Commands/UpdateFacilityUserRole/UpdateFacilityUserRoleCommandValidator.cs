using FluentValidation;
using Template.Domain.Constants;

namespace Template.Application.Facilities.Commands.UpdateFacilityUserRole;

public sealed class UpdateFacilityUserRoleCommandValidator : AbstractValidator<UpdateFacilityUserRoleCommand> {
    public UpdateFacilityUserRoleCommandValidator() {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole)
            .NotEmpty()
            .Must(role => Roles.FacilityRoles.Contains(role))
            .WithMessage("Role must be a valid facility role");
    }
}
