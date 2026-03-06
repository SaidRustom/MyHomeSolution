using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTodayTasks;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Queries.GetTodayTasks;

public sealed class GetTodayTasksQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateOnly Today = new(2025, 6, 15);

    public GetTodayTasksQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.Today.Returns(Today);
    }

    [Fact]
    public async Task Handle_ShouldReturnRecurringTasksScheduledToday()
    {
        await SeedRecurringTaskWithOccurrence("Today Task", Today, OccurrenceStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Today Task");
        result.First().Occurrences.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnOverduePastOccurrences()
    {
        var yesterday = Today.AddDays(-1);
        await SeedRecurringTaskWithOccurrence("Overdue Task", yesterday, OccurrenceStatus.Overdue);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Overdue Task");
    }

    [Fact]
    public async Task Handle_ShouldReturnPendingPastOccurrences()
    {
        var pastDate = Today.AddDays(-3);
        await SeedRecurringTaskWithOccurrence("Past Pending", pastDate, OccurrenceStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Past Pending");
    }

    [Fact]
    public async Task Handle_ShouldNotReturnCompletedPastOccurrences()
    {
        var pastDate = Today.AddDays(-3);
        await SeedRecurringTaskWithOccurrence("Completed Past", pastDate, OccurrenceStatus.Completed);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotReturnSkippedPastOccurrences()
    {
        var pastDate = Today.AddDays(-3);
        await SeedRecurringTaskWithOccurrence("Skipped Past", pastDate, OccurrenceStatus.Skipped);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotReturnFutureOccurrences()
    {
        var futureDate = Today.AddDays(7);
        await SeedRecurringTaskWithOccurrence("Future Task", futureDate, OccurrenceStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNonRecurringTaskDueToday()
    {
        await SeedNonRecurringTask("Due Today", Today);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Due Today");
        result.First().IsRecurring.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnOverdueNonRecurringTask()
    {
        var pastDate = Today.AddDays(-5);
        await SeedNonRecurringTask("Overdue Non-Recurring", pastDate);

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Overdue Non-Recurring");
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnOwnedTasks()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Other User Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = "other-user"
        };
        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = Today,
            Status = OccurrenceStatus.Pending
        };
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(occurrence);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetTodayTasksQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTodayTasksQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private async Task SeedRecurringTaskWithOccurrence(string title, DateOnly dueDate, OccurrenceStatus status)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = title,
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = "user-1"
        };
        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = dueDate,
            Status = status,
            AssignedToUserId = "user-1"
        };
        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.Add(occurrence);
        await context.SaveChangesAsync();
    }

    private async Task SeedNonRecurringTask(string title, DateOnly dueDate)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = title,
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = false,
            IsActive = true,
            DueDate = dueDate,
            CreatedBy = "user-1"
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
