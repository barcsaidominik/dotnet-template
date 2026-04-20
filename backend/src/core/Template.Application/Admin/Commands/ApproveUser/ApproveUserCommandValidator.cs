using FluentValidation;
using Template.Domain.Constants;

namespace Template.Application.Admin.Commands.ApproveUser;

public sealed class ApproveUserCommandValidator : AbstractValidator<ApproveUserCommand>
{
    public ApproveUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Roles.FacilityRoles.Contains(role) || role == Roles.SYSTEM_ADMIN)
            .WithMessage("Invalid role");
    }
}
