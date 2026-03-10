using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Queries.GetOccurrencesByDateRange;

public sealed class GetOccurrencesByDateRangeQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetOccurrencesByDateRangeQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.Today.Returns(new DateOnly(2025, 6, 10));
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnOccurrencesWithinRange()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 30)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o =>
        {
            o.DueDate.Should().BeOnOrAfter(new DateOnly(2025, 6, 1));
            o.DueDate.Should().BeOnOrBefore(new DateOnly(2025, 6, 30));
        });
    }

    [Fact]
    public async Task Handle_ShouldExcludeOccurrencesOutsideRange()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 7, 1),
            EndDate = new DateOnly(2025, 7, 31)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().DueDate.Should().Be(new DateOnly(2025, 7, 15));
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
            Status = OccurrenceStatus.Completed
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Status.Should().Be(OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldFilterByAssignedUser()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
            AssignedToUserId = "user-b"
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().AllSatisfy(o => o.AssignedToUserId.Should().Be("user-b"));
    }

    [Fact]
    public async Task Handle_ShouldIncludeTaskDetails()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 30)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().AllSatisfy(o =>
        {
            o.TaskTitle.Should().NotBeNullOrEmpty();
            o.TaskId.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderedByDueDate()
    {
        await SeedOccurrences();

        using var context = _factory.CreateContext();
        var handler = new GetOccurrencesByDateRangeQueryHandler(context, _currentUserService, _dateTimeProvider, _identityService);
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeInAscendingOrder(o => o.DueDate);
    }

    private async Task SeedOccurrences()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Calendar Task",
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
                DueDate = new DateOnly(2025, 6, 1),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-a"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 15),
                Status = OccurrenceStatus.Completed,
                AssignedToUserId = "user-b"
            },
            new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 7, 15),
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = "user-a"
            }
        );

        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
