using FluentValidation;

namespace MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).When(x => x.EstimatedDurationMinutes.HasValue)
            .WithMessage("Estimated duration must be positive.");

        RuleFor(x => x.DefaultBillCurrency)
            .MaximumLength(2).WithMessage("Default bill currency must not exceed 2 characters.");

        When(x => x.IsRecurring, () =>
        {
            RuleFor(x => x.RecurrenceType)
                .NotNull().WithMessage("Recurrence type is required for recurring tasks.");

            RuleFor(x => x.Interval)
                .NotNull().WithMessage("Interval is required for recurring tasks.")
                .GreaterThan(0).When(x => x.Interval.HasValue)
                .WithMessage("Interval must be positive.");

            RuleFor(x => x.RecurrenceStartDate)
                .NotNull().WithMessage("Start date is required for recurring tasks.");

            RuleFor(x => x.RecurrenceEndDate)
                .GreaterThan(x => x.RecurrenceStartDate)
                .When(x => x.RecurrenceEndDate.HasValue && x.RecurrenceStartDate.HasValue)
                .WithMessage("End date must be after start date.");

            RuleFor(x => x.AssigneeUserIds)
                .NotEmpty().WithMessage("At least one assignee is required for recurring tasks.");
        });

        When(x => !x.IsRecurring, () =>
        {
            RuleFor(x => x.DueDate)
                .NotNull().WithMessage("Due date is required for non-recurring tasks.");
        });

        When(x => x.AutoCreateBill, () =>
        {
            RuleFor(x => x.DefaultBillAmount)
                .NotNull().WithMessage("Bill amount is required when auto-create bill is enabled.")
                .GreaterThan(0).When(x => x.DefaultBillAmount.HasValue)
                .WithMessage("Bill amount must be positive.");

            RuleFor(x => x.IsRecurring)
                .Equal(true).WithMessage("Auto-create bill requires a recurring task.");
        });
    }
}
