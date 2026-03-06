using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

public sealed class AddShoppingItemFromBillItemCommandValidatorTests
{
    private readonly AddShoppingItemFromBillItemCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenShoppingListIdIsEmpty()
    {
        var command = CreateValidCommand() with { ShoppingListId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShoppingListId);
    }

    [Fact]
    public void ShouldHaveError_WhenBillItemIdIsEmpty()
    {
        var command = CreateValidCommand() with { BillItemId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BillItemId);
    }

    [Fact]
    public void ShouldHaveError_WhenQuantityOverrideIsZero()
    {
        var command = CreateValidCommand() with { QuantityOverride = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.QuantityOverride);
    }

    [Fact]
    public void ShouldHaveError_WhenUnitOverrideExceedsMaxLength()
    {
        var command = CreateValidCommand() with { UnitOverride = new string('A', 51) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UnitOverride);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenOverridesAreNull()
    {
        var command = CreateValidCommand() with { QuantityOverride = null, UnitOverride = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static AddShoppingItemFromBillItemCommand CreateValidCommand() => new()
    {
        ShoppingListId = Guid.CreateVersion7(),
        BillItemId = Guid.CreateVersion7(),
        QuantityOverride = 3,
        UnitOverride = "kg"
    };
}
