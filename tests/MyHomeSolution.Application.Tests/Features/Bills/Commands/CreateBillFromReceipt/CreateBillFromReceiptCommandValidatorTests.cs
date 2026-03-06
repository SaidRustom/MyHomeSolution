using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.CreateBillFromReceipt;

public sealed class CreateBillFromReceiptCommandValidatorTests
{
    private readonly CreateBillFromReceiptCommandValidator _validator = new();

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenFileNameIsEmpty()
    {
        var command = CreateValidCommand() with { FileName = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void ShouldHaveError_WhenContentTypeIsInvalid()
    {
        var command = CreateValidCommand() with { ContentType = "application/pdf" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void ShouldNotHaveError_WhenContentTypeIsWebp()
    {
        var command = CreateValidCommand() with { ContentType = "image/webp" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void ShouldHaveError_WhenFileSizeExceedsLimit()
    {
        var largeStream = new MemoryStream(new byte[21 * 1024 * 1024]);
        var command = CreateValidCommand() with { Content = largeStream };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void ShouldNotHaveError_WhenNoSplitsProvided()
    {
        var command = CreateValidCommand() with { Splits = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenSplitsHaveDuplicateUserIds()
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
    public void ShouldHaveError_WhenCustomPercentagesDoNotTotal100()
    {
        var command = CreateValidCommand() with
        {
            Splits =
            [
                new BillSplitRequest { UserId = "user-1", Percentage = 30m },
                new BillSplitRequest { UserId = "user-2", Percentage = 30m }
            ]
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Splits);
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

    private static CreateBillFromReceiptCommand CreateValidCommand() => new()
    {
        FileName = "receipt.jpg",
        ContentType = "image/jpeg",
        Content = new MemoryStream([0xFF, 0xD8, 0xFF])
    };
}
