using FluentValidation;

namespace MyHomeSolution.Application.Features.Budgets.Commands.EditBudgetOccurrenceAmount;

public sealed class EditBudgetOccurrenceAmountCommandValidator
    : AbstractValidator<EditBudgetOccurrenceAmountCommand>
{
    public EditBudgetOccurrenceAmountCommandValidator()
    {
        RuleFor(x => x.OccurrenceId)
            .NotEmpty().WithMessage("Occurrence id is required.");

        RuleFor(x => x.NewAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be zero or greater.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.TransferReason)
            .MaximumLength(1000).WithMessage("Transfer reason must not exceed 1000 characters.");

        RuleFor(x => x)
            .Must(x => x.TransferOccurrenceId != x.OccurrenceId)
            .When(x => x.TransferOccurrenceId.HasValue)
            .WithMessage("Cannot transfer to the same occurrence.");
    }
}
