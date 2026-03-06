using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.SendConnectionRequest;

public sealed class SendConnectionRequestCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public SendConnectionRequestCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _identityService.UserExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Fact]
    public async Task Handle_ShouldCreatePendingConnection()
    {
        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var connection = await assertContext.UserConnections.FirstOrDefaultAsync(uc => uc.Id == id);
        connection.Should().NotBeNull();
        connection!.RequesterId.Should().Be("user-1");
        connection.AddresseeId.Should().Be("user-2");
        connection.Status.Should().Be(ConnectionStatus.Pending);
        connection.RespondedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldPublishConnectionRequestSentEvent()
    {
        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ConnectionRequestSentEvent>(e =>
                e.RequesterId == "user-1" && e.AddresseeId == "user-2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSendingToSelf()
    {
        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-1" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAddresseeDoesNotExist()
    {
        _identityService.UserExistsAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(false);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "nonexistent" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAlreadyConnected()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already connected*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenPendingRequestAlreadyExists()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenReverseDirectionPendingRequestExists()
    {
        await SeedConnection("user-2", "user-1", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public async Task Handle_ShouldResendRequest_WhenPreviouslyDeclined()
    {
        var existing = await SeedConnection("user-1", "user-2", ConnectionStatus.Declined);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().Be(existing.Id);

        using var assertContext = _factory.CreateContext();
        var connection = await assertContext.UserConnections.FirstAsync(uc => uc.Id == id);
        connection.Status.Should().Be(ConnectionStatus.Pending);
        connection.RespondedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldResendRequest_WhenPreviouslyCancelled()
    {
        var existing = await SeedConnection("user-1", "user-2", ConnectionStatus.Cancelled);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().Be(existing.Id);

        using var assertContext = _factory.CreateContext();
        var connection = await assertContext.UserConnections.FirstAsync(uc => uc.Id == id);
        connection.Status.Should().Be(ConnectionStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldResendRequest_WhenPreviouslyRemoved()
    {
        var existing = await SeedConnection("user-1", "user-2", ConnectionStatus.Removed);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = new SendConnectionRequestCommand { AddresseeId = "user-2" };

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().Be(existing.Id);

        using var assertContext = _factory.CreateContext();
        var connection = await assertContext.UserConnections.FirstAsync(uc => uc.Id == id);
        connection.Status.Should().Be(ConnectionStatus.Pending);
        connection.RequesterId.Should().Be("user-1");
        connection.AddresseeId.Should().Be("user-2");
    }

    private SendConnectionRequestCommandHandler CreateHandler(TestDbContext context)
        => new(context, _currentUserService, _identityService, _publisher);

    private async Task<UserConnection> SeedConnection(
        string requesterId, string addresseeId, ConnectionStatus status)
    {
        using var context = _factory.CreateContext();
        var connection = new UserConnection
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = status
        };
        context.UserConnections.Add(connection);
        await context.SaveChangesAsync();
        return connection;
    }

    public void Dispose() => _factory.Dispose();
}
