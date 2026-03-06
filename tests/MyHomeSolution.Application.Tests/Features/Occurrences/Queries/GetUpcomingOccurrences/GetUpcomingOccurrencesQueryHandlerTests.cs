using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetUpcomingOccurrences;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Queries.GetUpcomingOccurrences;

public sealed class GetUpcomingOccurrencesQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateOnly Today = new(2025, 6, 15);

    public GetUpcomingOccurrencesQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.Today.Returns(Today);
    }

    [Fact]
    public async Task Handle_ShouldReturnPendingFutureOccurrences()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(o =>
        {
            o.Status.Should().Be(OccurrenceStatus.Pending);
            o.DueDate.Should().BeOnOrAfter(Today);
        });
    }

    [Fact]
    public async Task Handle_ShouldExcludePastOccurrences()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().NotContain(o => o.DueDate < Today);
    }

    [Fact]
    public async Task Handle_ShouldExcludeCompletedOccurrences()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().NotContain(o => o.Status == OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderedByDueDate()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeInAscendingOrder(o => o.DueDate);
    }

    [Fact]
    public async Task Handle_ShouldRespectPagination()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 1 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldIncludeTaskContextInResults()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetUpcomingOccurrencesQueryHandler(context, _currentUserService, _dateTimeProvider);
        var query = new GetUpcomingOccurrencesQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(o =>
        {
            o.TaskTitle.Should().NotBeNullOrEmpty();
            o.TaskId.Should().NotBeEmpty();
        });
    }

    private async Task SeedOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Upcoming Task",
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
                DueDate = Today.AddDays(-5),
                Status = OccurrenceStatus.Completed,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = Today,
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = Today.AddDays(7),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-1"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = Today.AddDays(14),
                Status = OccurrenceStatus.Completed,
                AssignedToUserId = "user-1"
            }
        );

        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
