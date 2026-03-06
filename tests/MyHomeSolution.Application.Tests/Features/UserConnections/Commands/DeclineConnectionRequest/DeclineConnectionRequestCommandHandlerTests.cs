using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Commands.DeclineConnectionRequest;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Commands.DeclineConnectionRequest;

public sealed class DeclineConnectionRequestCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public DeclineConnectionRequestCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("addressee-user");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldDeclinePendingConnection()
    {
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new DeclineConnectionRequestCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        await handler.Handle(
            new DeclineConnectionRequestCommand(connection.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.UserConnections.FirstAsync(uc => uc.Id == connection.Id);
        updated.Status.Should().Be(ConnectionStatus.Declined);
        updated.RespondedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new DeclineConnectionRequestCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new DeclineConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new DeclineConnectionRequestCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new DeclineConnectionRequestCommand(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotTheAddressee()
    {
        _currentUserService.UserId.Returns("other-user");
        var connection = await SeedPendingConnection("requester-user", "addressee-user");

        using var context = _factory.CreateContext();
        var handler = new DeclineConnectionRequestCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new DeclineConnectionRequestCommand(connection.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConnectionIsNotPending()
    {
        var connection = await SeedConnection("requester-user", "addressee-user", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new DeclineConnectionRequestCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new DeclineConnectionRequestCommand(connection.Id), CancellationToken.None);
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
