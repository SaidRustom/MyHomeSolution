using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Queries.GetConnections;
using MyHomeSolution.Application.Features.Users.Common;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.UserConnections.Queries.GetConnections;

public sealed class GetConnectionsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetConnectionsQueryHandlerTests()
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
    public async Task Handle_ShouldReturnAcceptedConnections_ByDefault()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);
        await SeedConnection("user-1", "user-3", ConnectionStatus.Pending);
        await SeedConnection("user-4", "user-1", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(c => c.Status.Should().Be(ConnectionStatus.Accepted));
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);
        await SeedConnection("user-1", "user-3", ConnectionStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20, Status = ConnectionStatus.Pending },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.First().AddresseeId.Should().Be("user-3");
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyCurrentUserConnections()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);
        await SeedConnection("user-3", "user-4", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldPopulateConnectedUserId_WhenCurrentUserIsRequester()
    {
        await SeedConnection("user-1", "user-2", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        result.Items.First().ConnectedUserId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldPopulateConnectedUserId_WhenCurrentUserIsAddressee()
    {
        await SeedConnection("user-2", "user-1", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        result.Items.First().ConnectedUserId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldEnrichWithUserDetails()
    {
        await SeedConnection("user-1", "friend-1", ConnectionStatus.Accepted);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        var item = result.Items.First();
        item.ConnectedUserName.Should().Be("friend-1 User");
        item.ConnectedUserEmail.Should().Be("friend-1@test.com");
    }

    [Fact]
    public async Task Handle_ShouldRespectPagination()
    {
        for (var i = 0; i < 5; i++)
        {
            await SeedConnection("user-1", $"friend-{i}", ConnectionStatus.Accepted);
        }

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 2 },
            CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var act = () => handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoConnections()
    {
        using var context = _factory.CreateContext();
        var handler = new GetConnectionsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetConnectionsQuery { PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
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
