using FluentAssertions;
using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.SendConnectionRequest;

public sealed class SendConnectionRequestCommandValidatorTests
{
    private readonly SendConnectionRequestCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenAddresseeIdIsEmpty()
    {
        var command = new SendConnectionRequestCommand { AddresseeId = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddresseeId);
    }

    [Fact]
    public void ShouldHaveError_WhenAddresseeIdIsNull()
    {
        var command = new SendConnectionRequestCommand { AddresseeId = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddresseeId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAddresseeIdIsProvided()
    {
        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddresseeId);
    }
}
