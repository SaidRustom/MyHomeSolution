using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Queries.GetOccurrencesByDateRange;

public sealed class GetOccurrencesByDateRangeOverdueTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetOccurrencesByDateRangeOverdueTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.Today.Returns(new DateOnly(2025, 6, 15));
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldIncludeOverdueOccurrences_WhenRangeIncludesToday()
    {
        await SeedOverdueOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(
            context, _currentUserService, _dateTimeProvider, _identityService);

        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 6, 15),
            EndDate = new DateOnly(2025, 6, 15)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        // Should include today's occurrence + overdue occurrence from June 1
        result.Should().HaveCount(2);
        result.Should().Contain(o => o.DueDate == new DateOnly(2025, 6, 1));
        result.Should().Contain(o => o.DueDate == new DateOnly(2025, 6, 15));
    }

    [Fact]
    public async Task Handle_ShouldExcludeCompletedOverdueOccurrences()
    {
        await SeedCompletedOverdueOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(
            context, _currentUserService, _dateTimeProvider, _identityService);

        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 6, 15),
            EndDate = new DateOnly(2025, 6, 15)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        // Only today's occurrence, the completed overdue should be excluded
        result.Should().HaveCount(1);
        result.First().DueDate.Should().Be(new DateOnly(2025, 6, 15));
    }

    [Fact]
    public async Task Handle_ShouldExcludeSkippedOverdueOccurrences()
    {
        await SeedSkippedOverdueOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(
            context, _currentUserService, _dateTimeProvider, _identityService);

        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 6, 15),
            EndDate = new DateOnly(2025, 6, 15)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().DueDate.Should().Be(new DateOnly(2025, 6, 15));
    }

    [Fact]
    public async Task Handle_ShouldNotIncludeOverdue_WhenRangeDoesNotIncludeToday()
    {
        await SeedOverdueOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(
            context, _currentUserService, _dateTimeProvider, _identityService);

        // Range is in the future, does not include today (June 15)
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 7, 1),
            EndDate = new DateOnly(2025, 7, 31)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    private async Task SeedOverdueOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Overdue Task",
            Priority = TaskPriority.High,
            Category = TaskCategory.Cleaning,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = "user-1"
        };

        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.AddRange(
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 1),   // Past-due, not completed
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 15),   // Today
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            }
        );
        await context.SaveChangesAsync();
    }

    private async Task SeedCompletedOverdueOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Completed Overdue Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = "user-1"
        };

        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.AddRange(
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 1),
                Status = OccurrenceStatus.Completed,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 15),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            }
        );
        await context.SaveChangesAsync();
    }

    private async Task SeedSkippedOverdueOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Skipped Overdue Task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = "user-1"
        };

        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.AddRange(
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 1),
                Status = OccurrenceStatus.Skipped,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 15),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            }
        );
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
