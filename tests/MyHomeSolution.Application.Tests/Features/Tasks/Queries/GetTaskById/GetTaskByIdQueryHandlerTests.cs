using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTaskById;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetTaskByIdQueryHandlerTests()
    {
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnTaskDetail()
    {
        var taskId = await SeedSimpleTask();

        using var context = _factory.CreateContext();
        var handler = new GetTaskByIdQueryHandler(context, _identityService);

        var result = await handler.Handle(new GetTaskByIdQuery(taskId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(taskId);
        result.Title.Should().Be("Test Task");
        result.Description.Should().Be("Description");
        result.Priority.Should().Be(TaskPriority.High);
        result.Category.Should().Be(TaskCategory.Cleaning);
        result.IsRecurring.Should().BeFalse();
        result.IsActive.Should().BeTrue();
        result.DueDate.Should().Be(new DateOnly(2025, 6, 15));
        result.AssignedToUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnTaskWithRecurrencePattern()
    {
        var taskId = await SeedRecurringTask();

        using var context = _factory.CreateContext();
        var handler = new GetTaskByIdQueryHandler(context, _identityService);

        var result = await handler.Handle(new GetTaskByIdQuery(taskId), CancellationToken.None);

        result.IsRecurring.Should().BeTrue();
        result.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.Type.Should().Be(RecurrenceType.Weekly);
        result.RecurrencePattern.Interval.Should().Be(1);
        result.RecurrencePattern.StartDate.Should().Be(new DateOnly(2025, 1, 1));
        result.RecurrencePattern.AssigneeUserIds.Should().BeEquivalentTo(["user-a", "user-b"]);
    }

    [Fact]
    public async Task Handle_ShouldReturnTaskWithOccurrences()
    {
        var taskId = await SeedTaskWithOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetTaskByIdQueryHandler(context, _identityService);

        var result = await handler.Handle(new GetTaskByIdQuery(taskId), CancellationToken.None);

        result.Occurrences.Should().HaveCount(2);
        result.Occurrences.First().DueDate.Should().Be(new DateOnly(2025, 1, 7));
        result.Occurrences.Last().DueDate.Should().Be(new DateOnly(2025, 1, 14));
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new GetTaskByIdQueryHandler(context, _identityService);

        var act = () => handler.Handle(
            new GetTaskByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskIsDeleted()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Deleted",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsDeleted = true
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetTaskByIdQueryHandler(context, _identityService);

        var act = () => handler.Handle(
            new GetTaskByIdQuery(task.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Guid> SeedSimpleTask()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Test Task",
            Description = "Description",
            Priority = TaskPriority.High,
            Category = TaskCategory.Cleaning,
            EstimatedDurationMinutes = 30,
            IsRecurring = false,
            IsActive = true,
            DueDate = new DateOnly(2025, 6, 15),
            AssignedToUserId = "user-1"
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> SeedRecurringTask()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Recurring Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsRecurring = true,
            IsActive = true
        };

        var pattern = new RecurrencePattern
        {
            HouseholdTaskId = task.Id,
            Type = RecurrenceType.Weekly,
            Interval = 1,
            StartDate = new DateOnly(2025, 1, 1)
        };
        pattern.Assignees.Add(new RecurrenceAssignee
        {
            RecurrencePatternId = pattern.Id,
            UserId = "user-a",
            Order = 0
        });
        pattern.Assignees.Add(new RecurrenceAssignee
        {
            RecurrencePatternId = pattern.Id,
            UserId = "user-b",
            Order = 1
        });

        task.RecurrencePattern = pattern;
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> SeedTaskWithOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Task with occurrences",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true
        };
        task.Occurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 14),
            Status = OccurrenceStatus.Pending,
            AssignedToUserId = "user-b"
        });
        task.Occurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 7),
            Status = OccurrenceStatus.Completed,
            AssignedToUserId = "user-a"
        });

        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    public void Dispose() => _factory.Dispose();
}
