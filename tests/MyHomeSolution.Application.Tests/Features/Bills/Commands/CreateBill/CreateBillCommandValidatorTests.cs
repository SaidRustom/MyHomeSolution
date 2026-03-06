using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandValidatorTests
{
    private readonly CreateBillCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenTitleIsEmpty()
    {
        var command = CreateValidCommand() with { Title = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void ShouldHaveError_WhenAmountIsZero()
    {
        var command = CreateValidCommand() with { Amount = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldHaveError_WhenAmountIsNegative()
    {
        var command = CreateValidCommand() with { Amount = -10m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldHaveError_WhenSplitsAreEmpty()
    {
        var command = CreateValidCommand() with { Splits = [] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldHaveError_WhenSplitHasEmptyUserId()
    {
        var command = CreateValidCommand() with
        {
            Splits = [new BillSplitRequest { UserId = "" }]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Splits[0].UserId");
    }

    [Fact]
    public void ShouldHaveError_WhenCustomPercentagesDoNotTotal100()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new BillSplitRequest { UserId = "user-1", Percentage = 30m },
                new BillSplitRequest { UserId = "user-2", Percentage = 40m }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldHaveError_WhenMixedCustomAndDefaultPercentages()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new BillSplitRequest { UserId = "user-1", Percentage = 60m },
                new BillSplitRequest { UserId = "user-2" }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldHaveError_WhenDuplicateUserIds()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new BillSplitRequest { UserId = "user-1" },
                new BillSplitRequest { UserId = "user-1" }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenCustomPercentagesTotal100()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new BillSplitRequest { UserId = "user-1", Percentage = 60m },
                new BillSplitRequest { UserId = "user-2", Percentage = 40m }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateBillCommand CreateValidCommand() => new()
    {
        Title = "Groceries",
        Amount = 100m,
        Currency = "USD",
        Category = Domain.Enums.BillCategory.Groceries,
        BillDate = DateTimeOffset.UtcNow,
        Splits =
        [
            new BillSplitRequest { UserId = "user-1" },
            new BillSplitRequest { UserId = "user-2" }
        ]
    };
}
