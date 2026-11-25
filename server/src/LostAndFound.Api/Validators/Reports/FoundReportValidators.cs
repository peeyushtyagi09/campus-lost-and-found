using FluentValidation;
using LostAndFound.Api.Dtos.Reports;

namespace LostAndFound.Api.Validators.Reports;

public sealed class CreateFoundReportRequestValidator : AbstractValidator<CreateFoundReportRequest>
{
    public CreateFoundReportRequestValidator()
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

        RuleFor(x => x.HeldAt)
            .NotEmpty()
            .Must(desk => !string.IsNullOrWhiteSpace(desk) && desk.Trim().Length is >= 2 and <= 80)
            .WithMessage("HeldAt desk name must be between 2 and 80 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("HeldAt must not contain email addresses or phone numbers.");

        RuleFor(x => x.DateFound)
            .Must(PublicFieldRules.IsValidPastOrTodayDate)
            .WithMessage("Date found must be a valid date not in the future.");

        RuleFor(x => x.PhotoFileName)
            .Must(PublicFieldRules.IsValidOptionalPhotoFileName)
            .WithMessage("Photo file name must reference a sanitized uploaded image.");

        RuleFor(x => x.StorageNotes).NotNull().MaximumLength(500);
        RuleFor(x => x.FinderContact).NotEmpty().MaximumLength(120);
        RuleFor(x => x.VerificationDetails).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UpdateFoundReportRequestValidator : AbstractValidator<UpdateFoundReportRequest>
{
    public UpdateFoundReportRequestValidator()
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

        RuleFor(x => x.HeldAt)
            .NotEmpty()
            .Must(desk => !string.IsNullOrWhiteSpace(desk) && desk.Trim().Length is >= 2 and <= 80)
            .WithMessage("HeldAt desk name must be between 2 and 80 characters.")
            .Must(PublicFieldRules.HasNoContactInfo)
            .WithMessage("HeldAt must not contain email addresses or phone numbers.");

        RuleFor(x => x.DateFound)
            .Must(PublicFieldRules.IsValidPastOrTodayDate)
            .WithMessage("Date found must be a valid date not in the future.");

        RuleFor(x => x.PhotoFileName)
            .Must(PublicFieldRules.IsValidOptionalPhotoFileName)
            .WithMessage("Photo file name must reference a sanitized uploaded image.");

        RuleFor(x => x.StorageNotes).NotNull().MaximumLength(500);
        RuleFor(x => x.FinderContact).NotEmpty().MaximumLength(120);
        RuleFor(x => x.VerificationDetails).NotEmpty().MaximumLength(1000);
    }
}
