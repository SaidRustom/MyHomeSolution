using FluentValidation;

namespace MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256).WithMessage("Title must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.ToUserId)
            .NotEmpty().WithMessage("Recipient user is required.")
            .MaximumLength(450).WithMessage("ToUserId must not exceed 450 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid notification type.");

        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(256).WithMessage("Related entity type must not exceed 256 characters.")
            .When(x => x.RelatedEntityType is not null);
    }
}
