using FluentValidation;

namespace Template.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        // Nem kell validáció, a frontend végzi.
    }
}
