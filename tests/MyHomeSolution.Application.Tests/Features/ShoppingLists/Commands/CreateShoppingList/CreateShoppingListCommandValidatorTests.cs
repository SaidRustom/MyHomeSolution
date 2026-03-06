using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.CreateShoppingList;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandValidatorTests
{
    private readonly CreateShoppingListCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenTitleIsEmpty()
    {
        var command = CreateValidCommand() with { Title = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void ShouldHaveError_WhenTitleExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Title = new string('A', 257) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Description = new string('A', 2001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenOptionalFieldsAreNull()
    {
        var command = new CreateShoppingListCommand
        {
            Title = "Minimal List",
            Category = ShoppingListCategory.General
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateShoppingListCommand CreateValidCommand() => new()
    {
        Title = "Groceries",
        Description = "Weekly groceries",
        Category = ShoppingListCategory.Groceries,
        DueDate = new DateOnly(2025, 7, 15)
    };
}
