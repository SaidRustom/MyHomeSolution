using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidatorTests
{
    private readonly UpdateTaskCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenIdIsEmpty()
    {
        var command = CreateValidCommand() with { Id = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void ShouldFail_WhenTitleIsEmpty()
    {
        var command = CreateValidCommand() with { Title = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void ShouldFail_WhenTitleExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Title = new string('X', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters.");
    }

    [Fact]
    public void ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Description = new string('X', 2001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ShouldFail_WhenEstimatedDurationIsNotPositive(int duration)
    {
        var command = CreateValidCommand() with { EstimatedDurationMinutes = duration };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EstimatedDurationMinutes);
    }

    [Fact]
    public void ShouldPass_WhenEstimatedDurationIsNull()
    {
        var command = CreateValidCommand() with { EstimatedDurationMinutes = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EstimatedDurationMinutes);
    }

    [Fact]
    public void ShouldPass_WhenDescriptionIsNull()
    {
        var command = CreateValidCommand() with { Description = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    private static UpdateTaskCommand CreateValidCommand() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Title = "Updated Task",
            Description = "Updated description",
            Priority = TaskPriority.High,
            Category = TaskCategory.Maintenance,
            EstimatedDurationMinutes = 60,
            IsActive = true,
            DueDate = new DateOnly(2025, 7, 1),
            AssignedToUserId = "user-1"
        };

    private static UpdateTaskCommand CreateValidRecurringCommand() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Title = "Recurring Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsActive = true,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["user-a"]
        };

    [Fact]
    public void ShouldPass_WhenRecurringCommandIsValid()
    {
        var command = CreateValidRecurringCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenRecurringButMissingRecurrenceType()
    {
        var command = CreateValidRecurringCommand() with { RecurrenceType = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceType);
    }

    [Fact]
    public void ShouldFail_WhenRecurringButMissingInterval()
    {
        var command = CreateValidRecurringCommand() with { Interval = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval);
    }

    [Fact]
    public void ShouldFail_WhenRecurringButMissingStartDate()
    {
        var command = CreateValidRecurringCommand() with { RecurrenceStartDate = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceStartDate);
    }

    [Fact]
    public void ShouldFail_WhenRecurringButNoAssignees()
    {
        var command = CreateValidRecurringCommand() with { AssigneeUserIds = [] };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AssigneeUserIds);
    }

    [Fact]
    public void ShouldFail_WhenEndDateBeforeStartDate()
    {
        var command = CreateValidRecurringCommand() with
        {
            RecurrenceEndDate = new DateOnly(2024, 12, 31)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceEndDate);
    }

    [Fact]
    public void ShouldPass_WhenAutoBillWithValidConfig()
    {
        var command = CreateValidRecurringCommand() with
        {
            AutoCreateBill = true,
            DefaultBillAmount = 100m
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenAutoBillButMissingAmount()
    {
        var command = CreateValidRecurringCommand() with
        {
            AutoCreateBill = true,
            DefaultBillAmount = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DefaultBillAmount);
    }

    [Fact]
    public void ShouldFail_WhenAutoBillOnNonRecurringTask()
    {
        var command = CreateValidCommand() with
        {
            AutoCreateBill = true,
            DefaultBillAmount = 50m
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IsRecurring);
    }
}
