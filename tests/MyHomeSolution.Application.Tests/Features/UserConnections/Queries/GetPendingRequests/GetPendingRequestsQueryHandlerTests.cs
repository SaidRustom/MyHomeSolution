using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Queries.GetPendingRequests;
using MyHomeSolution.Application.Features.Users.Common;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Queries.GetPendingRequests;

public sealed class GetPendingRequestsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetPendingRequestsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _identityService.GetUserByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var uid = callInfo.ArgAt<string>(0);
                return new UserDetailDto
                {
                    Id = uid,
                    Email = $"{uid}@test.com",
                    FirstName = uid,
                    LastName = "User",
                    FullName = $"{uid} User",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            });
    }

    [Fact]
    public async Task Handle_ShouldReturnReceivedPendingRequests()
    {
        await SeedConnection("user-2", "user-1", ConnectionStatus.Pending);
        await SeedConnection("user-3", "user-1", ConnectionStatus.Pending);
        await SeedConnection("user-1", "user-4", ConnectionStatus.Pending); // Sent, not received

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.AddresseeId.Should().Be("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldReturnSentPendingRequests()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Pending);
        await SeedConnection("user-1", "user-3", ConnectionStatus.Pending);
        await SeedConnection("user-4", "user-1", ConnectionStatus.Pending); // Received, not sent

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = true },
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.RequesterId.Should().Be("user-1"));
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnPendingStatus()
    {
        await SeedConnection("user-2", "user-1", ConnectionStatus.Pending);
        await SeedConnection("user-3", "user-1", ConnectionStatus.Accepted);
        await SeedConnection("user-4", "user-1", ConnectionStatus.Declined);

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().RequesterId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldSetConnectedUserId_ForReceivedRequests()
    {
        await SeedConnection("sender-user", "user-1", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);

        result.First().ConnectedUserId.Should().Be("sender-user");
    }

    [Fact]
    public async Task Handle_ShouldSetConnectedUserId_ForSentRequests()
    {
        await SeedConnection("user-1", "target-user", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = true },
            CancellationToken.None);

        result.First().ConnectedUserId.Should().Be("target-user");
    }

    [Fact]
    public async Task Handle_ShouldEnrichWithUserDetails()
    {
        await SeedConnection("friend-1", "user-1", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);

        result.First().ConnectedUserName.Should().Be("friend-1 User");
        result.First().ConnectedUserEmail.Should().Be("friend-1@test.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoPendingRequests()
    {
        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetPendingRequestsQueryHandler(context, _currentUserService, _identityService);

        var act = () => handler.Handle(
            new GetPendingRequestsQuery { Sent = false },
            CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task SeedConnection(string requesterId, string addresseeId, ConnectionStatus status)
    {
        using var context = _factory.CreateContext();
        context.UserConnections.Add(new UserConnection
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = status
        });
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
