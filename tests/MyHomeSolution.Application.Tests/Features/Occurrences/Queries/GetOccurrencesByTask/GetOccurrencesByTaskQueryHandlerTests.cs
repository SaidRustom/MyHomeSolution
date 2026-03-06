using FluentAssertions;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByTask;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Queries.GetOccurrencesByTask;

public sealed class GetOccurrencesByTaskQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldReturnPaginatedOccurrences()
    {
        var taskId = await SeedTaskWithOccurrences(3);

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery
        {
            HouseholdTaskId = taskId,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        var taskId = await SeedTaskWithMixedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery
        {
            HouseholdTaskId = taskId,
            Status = OccurrenceStatus.Completed
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.First().Status.Should().Be(OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedOccurrences()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 1),
            Status = OccurrenceStatus.Pending
        });
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 8),
            Status = OccurrenceStatus.Pending,
            IsDeleted = true
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery { HouseholdTaskId = task.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldOrderByDueDate()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Ordered task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true
        };
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 3, 1),
            Status = OccurrenceStatus.Pending
        });
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 1),
            Status = OccurrenceStatus.Pending
        });
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 2, 1),
            Status = OccurrenceStatus.Pending
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery { HouseholdTaskId = task.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        var dueDates = result.Items.Select(o => o.DueDate).ToList();
        dueDates.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_ShouldPaginateCorrectly()
    {
        var taskId = await SeedTaskWithOccurrences(5);

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery
        {
            HouseholdTaskId = taskId,
            PageNumber = 2,
            PageSize = 2
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDtoFields()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "DTO test",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true
        };
        var completedAt = new DateTimeOffset(2025, 2, 1, 14, 0, 0, TimeSpan.Zero);
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 2, 1),
            Status = OccurrenceStatus.Completed,
            AssignedToUserId = "user-x",
            CompletedAt = completedAt,
            Notes = "Done early"
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery { HouseholdTaskId = task.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        var dto = result.Items.Single();
        dto.DueDate.Should().Be(new DateOnly(2025, 2, 1));
        dto.Status.Should().Be(OccurrenceStatus.Completed);
        dto.AssignedToUserId.Should().Be("user-x");
        dto.CompletedAt.Should().Be(completedAt);
        dto.Notes.Should().Be("Done early");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoOccurrencesExist()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Empty task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByTaskQueryHandler(context);
        var query = new GetOccurrencesByTaskQuery { HouseholdTaskId = task.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private async Task<Guid> SeedTaskWithOccurrences(int count)
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
        context.HouseholdTasks.Add(task);

        for (var i = 0; i < count; i++)
        {
            context.TaskOccurrences.Add(new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 1, 1).AddDays(7 * i),
                Status = OccurrenceStatus.Pending
            });
        }

        await context.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> SeedTaskWithMixedOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Mixed task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true
        };
        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 1),
            Status = OccurrenceStatus.Pending
        });
        context.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 8),
            Status = OccurrenceStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow
        });
        context.TaskOccurrences.Add(new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 15),
            Status = OccurrenceStatus.Skipped
        });
        await context.SaveChangesAsync();
        return task.Id;
    }

    public void Dispose() => _factory.Dispose();
}
