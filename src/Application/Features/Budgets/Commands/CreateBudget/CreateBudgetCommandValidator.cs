using FluentValidation;

namespace MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;

public sealed class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Budget name is required.")
            .MaximumLength(256).WithMessage("Budget name must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(3).WithMessage("Currency code must not exceed 3 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.Period)
            .IsInEnum().WithMessage("Invalid budget period.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid budget category.");
    }
}
