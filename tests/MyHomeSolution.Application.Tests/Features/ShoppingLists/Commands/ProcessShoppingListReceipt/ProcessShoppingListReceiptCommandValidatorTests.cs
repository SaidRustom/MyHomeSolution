using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

public sealed class ProcessShoppingListReceiptCommandValidatorTests
{
    private readonly ProcessShoppingListReceiptCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenShoppingListIdIsEmpty()
    {
        var command = CreateValidCommand() with { ShoppingListId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShoppingListId);
    }

    [Fact]
    public void ShouldFail_WhenFileNameIsEmpty()
    {
        var command = CreateValidCommand() with { FileName = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void ShouldFail_WhenContentTypeIsInvalid()
    {
        var command = CreateValidCommand() with { ContentType = "application/pdf" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void ShouldPass_WhenContentTypeIsAllowed(string contentType)
    {
        var command = CreateValidCommand() with { ContentType = contentType };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void ShouldFail_WhenDuplicateSplitUserIds()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new ReceiptSplitRequest { UserId = "user-1" },
                new ReceiptSplitRequest { UserId = "user-1" }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits)
            .WithErrorMessage("Duplicate user ids in splits are not allowed.");
    }

    [Fact]
    public void ShouldFail_WhenPercentagesDoNotTotal100()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new ReceiptSplitRequest { UserId = "user-1", Percentage = 40m },
                new ReceiptSplitRequest { UserId = "user-2", Percentage = 40m }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldPass_WhenSplitsAreNull()
    {
        var command = CreateValidCommand() with { Splits = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Splits);
    }

    [Fact]
    public void ShouldPass_WhenSplitsHaveValidPercentages()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new ReceiptSplitRequest { UserId = "user-1", Percentage = 60m },
                new ReceiptSplitRequest { UserId = "user-2", Percentage = 40m }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ProcessShoppingListReceiptCommand CreateValidCommand() => new()
    {
        ShoppingListId = Guid.CreateVersion7(),
        FileName = "receipt.jpg",
        ContentType = "image/jpeg",
        Content = new MemoryStream([0xFF, 0xD8, 0xFF])
    };
}
