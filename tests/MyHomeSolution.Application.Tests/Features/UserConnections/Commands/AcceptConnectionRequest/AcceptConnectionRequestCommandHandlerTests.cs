using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Commands.AcceptConnectionRequest;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.AcceptConnectionRequest;

public sealed class AcceptConnectionRequestCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public AcceptConnectionRequestCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("addressee-user");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldAcceptPendingConnection()
    {
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.UserConnections.FirstAsync(uc => uc.Id == connection.Id);
        updated.Status.Should().Be(ConnectionStatus.Accepted);
        updated.RespondedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldPublishConnectionRequestAcceptedEvent()
    {
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ConnectionRequestAcceptedEvent>(e =>
                e.ConnectionId == connection.Id &&
                e.RequesterId == "requester-user" &&
                e.AcceptedByUserId == "addressee-user"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new AcceptConnectionRequestCommand(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotTheAddressee()
    {
        _currentUserService.UserId.Returns("other-user");
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsNotPending()
    {
        var connection = await SeedConnection("requester-user", "addressee-user", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*no longer pending*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsDeclined()
    {
        var connection = await SeedConnection("requester-user", "addressee-user", ConnectionStatus.Declined);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new AcceptConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    private AcceptConnectionRequestCommandHandler CreateHandler(TestDbContext context)
        => new(context, _currentUserService, _dateTimeProvider, _publisher);

    private Task<UserConnection> SeedPendingConnection(string requesterId, string addresseeId)
        => SeedConnection(requesterId, addresseeId, ConnectionStatus.Pending);

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
