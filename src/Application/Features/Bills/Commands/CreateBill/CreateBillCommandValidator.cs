using FluentValidation;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandValidator : AbstractValidator<CreateBillCommand>
{
    public CreateBillCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256).WithMessage("Title must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(3).WithMessage("Currency code must not exceed 3 characters.");

        RuleFor(x => x.BillDate)
            .NotEmpty().WithMessage("Bill date is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(256).WithMessage("Related entity type must not exceed 256 characters.");

        RuleFor(x => x.Splits)
            .NotEmpty().WithMessage("At least one split is required.");

        RuleForEach(x => x.Splits).ChildRules(split =>
        {
            split.RuleFor(s => s.UserId)
                .NotEmpty().WithMessage("Split user id is required.");

            split.RuleFor(s => s.Percentage)
                .GreaterThan(0).When(s => s.Percentage.HasValue)
                .WithMessage("Percentage must be greater than zero.")
                .LessThanOrEqualTo(100).When(s => s.Percentage.HasValue)
                .WithMessage("Percentage must not exceed 100.");
        });

        RuleFor(x => x.Splits)
            .Must(splits =>
            {
                var customPercentages = splits.Where(s => s.Percentage.HasValue).ToList();
                if (customPercentages.Count == 0)
                    return true;

                if (customPercentages.Count != splits.Count)
                    return false;

                var total = customPercentages.Sum(s => s.Percentage!.Value);
                return Math.Abs(total - 100m) < 0.01m;
            })
            .WithMessage("When custom percentages are specified, all splits must have a percentage and they must total 100%.");

        RuleFor(x => x.Splits)
            .Must(splits => splits.Select(s => s.UserId).Distinct().Count() == splits.Count)
            .WithMessage("Duplicate user ids in splits are not allowed.");
    }
}
