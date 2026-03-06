using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Commands.RemoveConnection;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.RemoveConnection;

public sealed class RemoveConnectionCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public RemoveConnectionCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAcceptedConnection_AsRequester()
    {
        var connection = await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(new RemoveConnectionCommand(connection.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.UserConnections.FirstAsync(uc => uc.Id == connection.Id);
        updated.Status.Should().Be(ConnectionStatus.Removed);
        updated.RespondedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAcceptedConnection_AsAddressee()
    {
        var connection = await SeedConnection("user-2", "user-1", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(new RemoveConnectionCommand(connection.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.UserConnections.FirstAsync(uc => uc.Id == connection.Id);
        updated.Status.Should().Be(ConnectionStatus.Removed);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);
        var connection = await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new RemoveConnectionCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new RemoveConnectionCommand(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotPartyToConnection()
    {
        _currentUserService.UserId.Returns("unrelated-user");
        var connection = await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new RemoveConnectionCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsNotAccepted()
    {
        var connection = await SeedConnection("user-1", "user-2", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new RemoveConnectionCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Only accepted connections*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsDeclined()
    {
        var connection = await SeedConnection("user-1", "user-2", ConnectionStatus.Declined);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(
            new RemoveConnectionCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    private RemoveConnectionCommandHandler CreateHandler(TestDbContext context)
        => new(context, _currentUserService, _dateTimeProvider);

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
