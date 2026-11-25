using System.Text.RegularExpressions;
using FluentValidation;
using LostAndFound.Api.Dtos.Auth;

namespace LostAndFound.Api.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private static readonly Regex UppercaseRegex = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new("[0-9]", RegexOptions.Compiled);

    public RegisterRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= 2 and <= 60)
            .WithMessage("Display name must be between 2 and 60 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128)
            .Must(p => UppercaseRegex.IsMatch(p))
            .WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => LowercaseRegex.IsMatch(p))
            .WithMessage("Password must contain at least one lowercase letter.")
            .Must(p => DigitRegex.IsMatch(p))
            .WithMessage("Password must contain at least one digit.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
