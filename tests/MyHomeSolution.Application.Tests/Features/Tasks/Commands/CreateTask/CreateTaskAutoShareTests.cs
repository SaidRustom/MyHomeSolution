using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskAutoShareTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IOccurrenceScheduler _occurrenceScheduler = Substitute.For<IOccurrenceScheduler>();

    public CreateTaskAutoShareTests()
    {
        _currentUserService.UserId.Returns("creator-user");
    }

    [Fact]
    public async Task Handle_ShouldCreateEntityShare_WhenAssignedToDifferentUser()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Assigned task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 6, 15),
            AssignedToUserId = "other-user"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var shares = await assertContext.EntityShares
            .Where(s => s.EntityId == id && s.EntityType == EntityTypes.HouseholdTask)
            .ToListAsync();

        shares.Should().HaveCount(1);
        shares[0].SharedWithUserId.Should().Be("other-user");
        shares[0].Permission.Should().Be(SharePermission.Edit);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateEntityShare_WhenAssignedToSelf()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Self-assigned task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 6, 15),
            AssignedToUserId = "creator-user"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var shares = await assertContext.EntityShares
            .Where(s => s.EntityId == id && s.EntityType == EntityTypes.HouseholdTask)
            .ToListAsync();

        shares.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotCreateEntityShare_WhenNoAssignee()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Unassigned task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 6, 15),
            AssignedToUserId = null
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var shares = await assertContext.EntityShares
            .Where(s => s.EntityId == id && s.EntityType == EntityTypes.HouseholdTask)
            .ToListAsync();

        shares.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCreateEntityShares_ForAllRecurrenceAssignees()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Rotating task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["creator-user", "user-a", "user-b"]
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var shares = await assertContext.EntityShares
            .Where(s => s.EntityId == id && s.EntityType == EntityTypes.HouseholdTask)
            .ToListAsync();

        // creator-user should NOT be shared with themselves
        shares.Should().HaveCount(2);
        shares.Select(s => s.SharedWithUserId).Should().BeEquivalentTo(["user-a", "user-b"]);
        shares.Should().AllSatisfy(s => s.Permission.Should().Be(SharePermission.Edit));
    }

    [Fact]
    public async Task Handle_ShouldNotDuplicateShare_WhenAssigneeIsAlsoInRotation()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Task with overlap",
            Priority = TaskPriority.High,
            Category = TaskCategory.General,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssignedToUserId = "other-user",
            AssigneeUserIds = ["other-user", "user-b"]
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var shares = await assertContext.EntityShares
            .Where(s => s.EntityId == id && s.EntityType == EntityTypes.HouseholdTask)
            .ToListAsync();

        // other-user gets share from AssignedToUserId, user-b gets share from rotation
        // other-user should NOT be duplicated
        shares.Should().HaveCount(2);
        shares.Select(s => s.SharedWithUserId).Should().BeEquivalentTo(["other-user", "user-b"]);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
