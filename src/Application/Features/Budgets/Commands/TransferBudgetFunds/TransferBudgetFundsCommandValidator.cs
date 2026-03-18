using FluentValidation;

namespace MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;

public sealed class TransferBudgetFundsCommandValidator
    : AbstractValidator<TransferBudgetFundsCommand>
{
    public TransferBudgetFundsCommandValidator()
    {
        RuleFor(x => x.SourceOccurrenceId)
            .NotEmpty().WithMessage("Source occurrence id is required.");

        RuleFor(x => x.DestinationOccurrenceId)
            .NotEmpty().WithMessage("Destination occurrence id is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than zero.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");

        RuleFor(x => x)
            .Must(x => x.SourceOccurrenceId != x.DestinationOccurrenceId)
            .WithMessage("Source and destination occurrences must be different.");
    }
}
