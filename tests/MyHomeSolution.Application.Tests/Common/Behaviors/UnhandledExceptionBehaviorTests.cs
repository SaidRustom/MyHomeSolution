using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyHomeSolution.Application.Common.Behaviors;

namespace MyHomeSolution.Application.Tests.Common.Behaviors;

public sealed record UnhandledTestRequest(string Value) : IRequest<UnhandledTestResponse>;
public sealed record UnhandledTestResponse(int Result);

public sealed class UnhandledExceptionBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldReturnResponse_WhenNoExceptionOccurs()
    {
        var logger = NullLogger<UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>>.Instance;
        var behavior = new UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>(logger);
        var expectedResponse = new UnhandledTestResponse(100);

        var result = await behavior.Handle(
            new UnhandledTestRequest("ok"),
            _ => Task.FromResult(expectedResponse),
            CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldRethrow_WhenExceptionOccurs()
    {
        var logger = NullLogger<UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>>.Instance;
        var behavior = new UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>(logger);

        var act = () => behavior.Handle(
            new UnhandledTestRequest("fail"),
            _ => throw new InvalidOperationException("Something broke"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Something broke");
    }

    [Fact]
    public async Task Handle_ShouldNotSwallowException()
    {
        var logger = NullLogger<UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>>.Instance;
        var behavior = new UnhandledExceptionBehavior<UnhandledTestRequest, UnhandledTestResponse>(logger);
        var originalException = new ArgumentNullException("param");

        var act = () => behavior.Handle(
            new UnhandledTestRequest("fail"),
            _ => throw originalException,
            CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentNullException>())
            .Which.Should().BeSameAs(originalException);
    }
}
