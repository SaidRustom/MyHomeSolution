using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenNonRecurringCommandIsValid()
    {
        var command = CreateValidNonRecurringCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldPass_WhenRecurringCommandIsValid()
    {
        var command = CreateValidRecurringCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenTitleIsEmpty()
    {
        var command = CreateValidNonRecurringCommand() with { Title = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void ShouldFail_WhenTitleExceedsMaxLength()
    {
        var command = CreateValidNonRecurringCommand() with { Title = new string('A', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters.");
    }

    [Fact]
    public void ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateValidNonRecurringCommand() with { Description = new string('A', 2001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ShouldFail_WhenEstimatedDurationIsNotPositive(int duration)
    {
        var command = CreateValidNonRecurringCommand() with { EstimatedDurationMinutes = duration };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EstimatedDurationMinutes);
    }

    [Fact]
    public void ShouldPass_WhenEstimatedDurationIsNull()
    {
        var command = CreateValidNonRecurringCommand() with { EstimatedDurationMinutes = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EstimatedDurationMinutes);
    }

    [Fact]
    public void ShouldFail_WhenRecurring_AndRecurrenceTypeIsMissing()
    {
        var command = CreateValidRecurringCommand() with { RecurrenceType = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceType)
            .WithErrorMessage("Recurrence type is required for recurring tasks.");
    }

    [Fact]
    public void ShouldFail_WhenRecurring_AndIntervalIsZero()
    {
        var command = CreateValidRecurringCommand() with { Interval = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval);
    }

    [Fact]
    public void ShouldFail_WhenRecurring_AndStartDateIsMissing()
    {
        var command = CreateValidRecurringCommand() with { RecurrenceStartDate = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceStartDate)
            .WithErrorMessage("Start date is required for recurring tasks.");
    }

    [Fact]
    public void ShouldFail_WhenRecurring_AndEndDateIsBeforeStartDate()
    {
        var command = CreateValidRecurringCommand() with
        {
            RecurrenceStartDate = new DateOnly(2025, 6, 1),
            RecurrenceEndDate = new DateOnly(2025, 5, 1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceEndDate)
            .WithErrorMessage("End date must be after start date.");
    }

    [Fact]
    public void ShouldFail_WhenRecurring_AndAssigneesAreEmpty()
    {
        var command = CreateValidRecurringCommand() with { AssigneeUserIds = [] };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AssigneeUserIds)
            .WithErrorMessage("At least one assignee is required for recurring tasks.");
    }

    [Fact]
    public void ShouldFail_WhenNonRecurring_AndDueDateIsMissing()
    {
        var command = CreateValidNonRecurringCommand() with { DueDate = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("Due date is required for non-recurring tasks.");
    }

    [Fact]
    public void ShouldNotRequireRecurrenceFields_WhenNotRecurring()
    {
        var command = CreateValidNonRecurringCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RecurrenceType);
        result.ShouldNotHaveValidationErrorFor(x => x.Interval);
        result.ShouldNotHaveValidationErrorFor(x => x.RecurrenceStartDate);
        result.ShouldNotHaveValidationErrorFor(x => x.AssigneeUserIds);
    }

    private static CreateTaskCommand CreateValidNonRecurringCommand() =>
        new()
        {
            Title = "Clean the kitchen",
            Description = "Deep clean all surfaces",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            EstimatedDurationMinutes = 30,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 6, 15)
        };

    private static CreateTaskCommand CreateValidRecurringCommand() =>
        new()
        {
            Title = "Vacuum the house",
            Priority = TaskPriority.High,
            Category = TaskCategory.Cleaning,
            EstimatedDurationMinutes = 45,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            RecurrenceEndDate = new DateOnly(2025, 12, 31),
            AssigneeUserIds = ["user-1", "user-2"]
        };
}
