using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.CreateBudget;

public sealed class CreateBudgetCommandValidatorTests
{
    private readonly CreateBudgetCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenValidCommand()
    {
        var command = new CreateBudgetCommand
        {
            Name = "Groceries",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenNameEmpty()
    {
        var command = new CreateBudgetCommand
        {
            Name = "",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var command = new CreateBudgetCommand
        {
            Name = "Test",
            Amount = 0m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenEndDateBeforeStartDate()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new CreateBudgetCommand
        {
            Name = "Test",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = now,
            EndDate = now.AddDays(-1)
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void ShouldPass_WhenEndDateNull()
    {
        var command = new CreateBudgetCommand
        {
            Name = "Test",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
