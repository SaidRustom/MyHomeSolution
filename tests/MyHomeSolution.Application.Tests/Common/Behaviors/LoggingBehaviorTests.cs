using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyHomeSolution.Application.Common.Behaviors;

namespace MyHomeSolution.Application.Tests.Common.Behaviors;

public sealed record LoggingTestRequest(string Value) : IRequest<LoggingTestResponse>;
public sealed record LoggingTestResponse(int Result);

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResponse()
    {
        var logger = NullLogger<LoggingBehavior<LoggingTestRequest, LoggingTestResponse>>.Instance;
        var behavior = new LoggingBehavior<LoggingTestRequest, LoggingTestResponse>(logger);
        var expectedResponse = new LoggingTestResponse(42);

        var result = await behavior.Handle(
            new LoggingTestRequest("test"),
            _ => Task.FromResult(expectedResponse),
            CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldInvokeNextExactlyOnce()
    {
        var logger = NullLogger<LoggingBehavior<LoggingTestRequest, LoggingTestResponse>>.Instance;
        var behavior = new LoggingBehavior<LoggingTestRequest, LoggingTestResponse>(logger);
        var callCount = 0;

        await behavior.Handle(
            new LoggingTestRequest("test"),
            _ =>
            {
                callCount++;
                return Task.FromResult(new LoggingTestResponse(1));
            },
            CancellationToken.None);

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenNextThrows()
    {
        var logger = NullLogger<LoggingBehavior<LoggingTestRequest, LoggingTestResponse>>.Instance;
        var behavior = new LoggingBehavior<LoggingTestRequest, LoggingTestResponse>(logger);

        var act = () => behavior.Handle(
            new LoggingTestRequest("test"),
            _ => throw new InvalidOperationException("Boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Boom");
    }
}
