using FluentValidation;
using LostAndFound.Api.Dtos.Claims;

namespace LostAndFound.Api.Validators.Claims;

public sealed class CreateClaimRequestValidator : AbstractValidator<CreateClaimRequest>
{
    public CreateClaimRequestValidator()
    {
        RuleFor(x => x.FoundReportId).NotEmpty();
        RuleFor(x => x.VerificationAnswers)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(1500);
    }
}

public sealed class ReviewClaimRequestValidator : AbstractValidator<ReviewClaimRequest>
{
    public ReviewClaimRequestValidator()
    {
        RuleFor(x => x.ReviewNotes).MaximumLength(500);
    }
}

public sealed class CompleteHandoverRequestValidator : AbstractValidator<CompleteHandoverRequest>
{
    public CompleteHandoverRequestValidator()
    {
        RuleFor(x => x.WitnessConfirmed)
            .Equal(true)
            .WithMessage("Witness confirmation is required to complete handover.");
        RuleFor(x => x.IdChecked)
            .Equal(true)
            .WithMessage("Government or campus ID check confirmation is required.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
