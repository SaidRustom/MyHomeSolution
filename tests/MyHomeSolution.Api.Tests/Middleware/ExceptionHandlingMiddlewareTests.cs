using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Api.Middleware;
using MyHomeSolution.Application.Common.Exceptions;
using NSubstitute;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Api.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger =
        Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenNoExceptionOccurs()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn404_WhenNotFoundExceptionIsThrown()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("HouseholdTask", Guid.Empty),
            _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().Be("application/problem+json");

        var body = await ReadResponseBody(context);
        body.Should().Contain("Not Found");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenValidationExceptionIsThrown()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Title", "Title is required.")
        };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(failures),
            _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");

        var body = await ReadResponseBody(context);
        body.Should().Contain("Validation Error");
        body.Should().Contain("Title");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_WhenUnhandledExceptionIsThrown()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Something went wrong"),
            _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/problem+json");

        var body = await ReadResponseBody(context);
        body.Should().Contain("Internal Server Error");
        body.Should().NotContain("Something went wrong",
            because: "internal error details should not be exposed");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn499_WhenOperationCancelledDueToClientDisconnect()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(),
            _logger);
        var context = CreateHttpContext();
        context.RequestAborted = cts.Token;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeErrorsDictionary_WhenValidationExceptionHasMultipleErrors()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Title", "Title is required."),
            new FluentValidation.Results.ValidationFailure("Title", "Title must not exceed 200 characters."),
            new FluentValidation.Results.ValidationFailure("DueDate", "Due date is required.")
        };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(failures),
            _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBody(context);
        using var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetProperty("Title").GetArrayLength().Should().Be(2);
        errors.GetProperty("DueDate").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenUnhandledExceptionOccurs()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Unexpected"),
            _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
