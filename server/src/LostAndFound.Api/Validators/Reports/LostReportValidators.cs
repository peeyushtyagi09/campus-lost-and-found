using System.Text.RegularExpressions;
using FluentValidation;
using LostAndFound.Api.Dtos.Reports;

namespace LostAndFound.Api.Validators.Reports;

public static class PublicFieldRules
{
    private static readonly Regex EmailRegex = new(
        @"[^\s@]+@[^\s@]+\.[^\s@]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneCandidateRegex = new(
        @"(?<!\d)(?:\+?\d[\d\s().-]{8,}\d)(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhotoFileNameRegex = new(
        @"^[0-9a-f]{32}\.(jpg|png|webp)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool HasNoContactInfo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (EmailRegex.IsMatch(value))
        {
            return false;
        }

        foreach (Match match in PhoneCandidateRegex.Matches(value))
        {
            var digitCount = match.Value.Count(char.IsDigit);
            if (digitCount >= 10)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidOptionalPhotoFileName(string? value)
        => string.IsNullOrWhiteSpace(value) || PhotoFileNameRegex.IsMatch(value);

    public static bool IsValidPastOrTodayDate(DateOnly date)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        return date >= new DateOnly(2020, 1, 1) && date <= todayUtc;
    }
}

public sealed class CreateLostReportRequestValidator : AbstractValidator<CreateLostReportRequest>
{
    public CreateLostReportRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Must(title => !string.IsNullOrWhiteSpace(title) && title.Trim().Length is >= 3 and <= 100)
            .WithMessage("Title must be between 3 and 100 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Title must not contain email addresses or phone numbers.");

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.PublicDescription)
            .NotNull()
            .MaximumLength(500)
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Public description must not contain email addresses or phone numbers.");

        RuleFor(x => x.CampusArea)
            .NotEmpty()
            .Must(area => !string.IsNullOrWhiteSpace(area) && area.Trim().Length is >= 2 and <= 80)
            .WithMessage("Campus area must be between 2 and 80 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Campus area must not contain email addresses or phone numbers.");

        RuleFor(x => x.DateLost)
            .Must(PublicFieldRules.IsValidPastOrTodayDate)
            .WithMessage("Date lost must be a valid date not in the future.");

        RuleFor(x => x.PhotoFileName)
            .Must(PublicFieldRules.IsValidOptionalPhotoFileName)
            .WithMessage("Photo file name must reference a sanitized uploaded image.");

        RuleFor(x => x.PrivateDetails)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}

public sealed class UpdateLostReportRequestValidator : AbstractValidator<UpdateLostReportRequest>
{
    public UpdateLostReportRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Must(title => !string.IsNullOrWhiteSpace(title) && title.Trim().Length is >= 3 and <= 100)
            .WithMessage("Title must be between 3 and 100 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Title must not contain email addresses or phone numbers.");

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.PublicDescription)
            .NotNull()
            .MaximumLength(500)
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Public description must not contain email addresses or phone numbers.");

        RuleFor(x => x.CampusArea)
            .NotEmpty()
            .Must(area => !string.IsNullOrWhiteSpace(area) && area.Trim().Length is >= 2 and <= 80)
            .WithMessage("Campus area must be between 2 and 80 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("Campus area must not contain email addresses or phone numbers.");

        RuleFor(x => x.DateLost)
            .Must(PublicFieldRules.IsValidPastOrTodayDate)
            .WithMessage("Date lost must be a valid date not in the future.");

        RuleFor(x => x.PhotoFileName)
            .Must(PublicFieldRules.IsValidOptionalPhotoFileName)
            .WithMessage("Photo file name must reference a sanitized uploaded image.");

        RuleFor(x => x.PrivateDetails)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}
