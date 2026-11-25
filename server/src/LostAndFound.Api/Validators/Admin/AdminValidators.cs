using System.Text.RegularExpressions;
using FluentValidation;
using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Validators.Admin;

public sealed class ApproveDisposalRequestValidator : AbstractValidator<ApproveDisposalRequest>
{
    public ApproveDisposalRequestValidator()
    {
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    private static readonly Regex UppercaseRegex = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new("[0-9]", RegexOptions.Compiled);

    public CreateStaffRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= 2 and <= 60);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128)
            .Must(p => UppercaseRegex.IsMatch(p))
            .Must(p => LowercaseRegex.IsMatch(p))
            .Must(p => DigitRegex.IsMatch(p));
    }
}

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => r is UserRole.User or UserRole.Staff)
            .WithMessage("Role can only be set to User or Staff.");
    }
}
