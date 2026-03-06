using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItem;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.AddShoppingItem;

public sealed class AddShoppingItemCommandValidatorTests
{
    private readonly AddShoppingItemCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenShoppingListIdIsEmpty()
    {
        var command = CreateValidCommand() with { ShoppingListId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShoppingListId);
    }

    [Fact]
    public void ShouldHaveError_WhenNameIsEmpty()
    {
        var command = CreateValidCommand() with { Name = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Name = new string('A', 501) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenQuantityIsZero()
    {
        var command = CreateValidCommand() with { Quantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void ShouldHaveError_WhenUnitExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Unit = new string('A', 51) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void ShouldHaveError_WhenNotesExceedMaxLength()
    {
        var command = CreateValidCommand() with { Notes = new string('A', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static AddShoppingItemCommand CreateValidCommand() => new()
    {
        ShoppingListId = Guid.CreateVersion7(),
        Name = "Milk",
        Quantity = 2,
        Unit = "liters",
        Notes = "Whole milk"
    };
}
