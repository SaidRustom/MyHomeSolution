using FluentValidation;

namespace MyHomeSolution.Application.Features.Shares.Commands.UpdateSharePermission;

public sealed class UpdateSharePermissionCommandValidator
    : AbstractValidator<UpdateSharePermissionCommand>
{
    public UpdateSharePermissionCommandValidator()
    {
        RuleFor(x => x.ShareId)
            .NotEmpty().WithMessage("Share ID is required.");

        RuleFor(x => x.Permission)
            .IsInEnum().WithMessage("Invalid share permission.");
    }
}
