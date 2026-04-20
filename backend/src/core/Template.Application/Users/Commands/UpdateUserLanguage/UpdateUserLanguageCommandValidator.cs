using FluentValidation;

namespace Template.Application.Users.Commands.UpdateUserLanguage;

public sealed class UpdateUserLanguageCommandValidator : AbstractValidator<UpdateUserLanguageCommand>
{
    private static readonly string[] _supportedLanguages = ["hu-HU", "en-US"];

    public UpdateUserLanguageCommandValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(lang => _supportedLanguages.Contains(lang))
            .WithErrorCode("Language.Invalid");
    }
}
