using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Commands.CancelConnectionRequest;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.CancelConnectionRequest;

public sealed class CancelConnectionRequestCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public CancelConnectionRequestCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("requester-user");
    }

    [Fact]
    public async Task Handle_ShouldCancelPendingConnection()
    {
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new CancelConnectionRequestCommandHandler(context, _currentUserService);

        await handler.Handle(
            new CancelConnectionRequestCommand(connection.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.UserConnections.FirstAsync(uc => uc.Id == connection.Id);
        updated.Status.Should().Be(ConnectionStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new CancelConnectionRequestCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new CancelConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new CancelConnectionRequestCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new CancelConnectionRequestCommand(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotTheRequester()
    {
        _currentUserService.UserId.Returns("addressee-user");
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new CancelConnectionRequestCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new CancelConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsNotPending()
    {
        var connection = await SeedConnection("requester-user", "addressee-user", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new CancelConnectionRequestCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new CancelConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

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
