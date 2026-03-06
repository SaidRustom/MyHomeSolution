using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MyHomeSolution.Application.Common.Behaviors;
using NSubstitute;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Application.Tests.Common.Behaviors;

public sealed record ValidationTestRequest(string Name) : IRequest<ValidationTestResponse>;
public sealed record ValidationTestResponse(int Value);

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsExist()
    {
        var validators = Enumerable.Empty<IValidator<ValidationTestRequest>>();
        var behavior = new ValidationBehavior<ValidationTestRequest, ValidationTestResponse>(validators);
        var request = new ValidationTestRequest("test");
        var expectedResponse = new ValidationTestResponse(42);

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(expectedResponse),
            CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<ValidationTestRequest, ValidationTestResponse>([validator]);
        var expectedResponse = new ValidationTestResponse(99);

        var result = await behavior.Handle(
            new ValidationTestRequest("valid"),
            _ => Task.FromResult(expectedResponse),
            CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(
            [
                new ValidationFailure("Name", "Name is required.")
            ]));

        var behavior = new ValidationBehavior<ValidationTestRequest, ValidationTestResponse>([validator]);

        var act = () => behavior.Handle(
            new ValidationTestRequest(""),
            _ => Task.FromResult(new ValidationTestResponse(0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldAggregateErrors_FromMultipleValidators()
    {
        var validator1 = Substitute.For<IValidator<ValidationTestRequest>>();
        validator1
            .ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Too short")]));

        var validator2 = Substitute.For<IValidator<ValidationTestRequest>>();
        validator2
            .ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Contains invalid chars")]));

        var behavior = new ValidationBehavior<ValidationTestRequest, ValidationTestResponse>([validator1, validator2]);

        var act = () => behavior.Handle(
            new ValidationTestRequest(""),
            _ => Task.FromResult(new ValidationTestResponse(0)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
        exception.Which.Errors["Name"].Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldNotCallNext_WhenValidationFails()
    {
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Required")]));

        var behavior = new ValidationBehavior<ValidationTestRequest, ValidationTestResponse>([validator]);
        var nextCalled = false;

        var act = () => behavior.Handle(
            new ValidationTestRequest(""),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(new ValidationTestResponse(0));
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }
}
