using FluentValidation;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.Shares.Commands.ShareEntity;

public sealed class ShareEntityCommandValidator : AbstractValidator<ShareEntityCommand>
{
    private static readonly string[] SupportedEntityTypes = [EntityTypes.HouseholdTask];

    public ShareEntityCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Entity type is required.")
            .Must(et => SupportedEntityTypes.Contains(et))
            .WithMessage($"Entity type must be one of: {string.Join(", ", SupportedEntityTypes)}.");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required.");

        RuleFor(x => x.SharedWithUserId)
            .NotEmpty().WithMessage("User ID to share with is required.");

        RuleFor(x => x.Permission)
            .IsInEnum().WithMessage("Invalid share permission.");
    }
}
