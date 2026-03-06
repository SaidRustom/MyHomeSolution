using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTasks;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryHandlerTests : IDisposable
{
    private const string TestUserId = "test-user";
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService;

    public GetTasksQueryHandlerTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(TestUserId);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedList()
    {
        await SeedMultipleTasks(5);

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldPaginateCorrectly()
    {
        await SeedMultipleTasks(5);

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { PageNumber = 1, PageSize = 2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategory()
    {
        await SeedTasksWithCategories();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { Category = TaskCategory.Cleaning };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(t => t.Category.Should().Be(TaskCategory.Cleaning));
    }

    [Fact]
    public async Task Handle_ShouldFilterByPriority()
    {
        await SeedTasksWithPriorities();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { Priority = TaskPriority.Critical };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(t => t.Priority.Should().Be(TaskPriority.Critical));
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsRecurring()
    {
        await SeedMixedRecurringTasks();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { IsRecurring = true };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(t => t.IsRecurring.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldFilterByAssignedUser()
    {
        await SeedTasksWithAssignees();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { AssignedToUserId = "user-a" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(t => t.AssignedToUserId.Should().Be("user-a"));
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        await SeedTasksForSearch();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery { SearchTerm = "kitchen" };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.First().Title.Should().Contain("kitchen");
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedTasks()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Active task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            CreatedBy = TestUserId
        });
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Deleted task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            IsDeleted = true,
            CreatedBy = TestUserId
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.First().Title.Should().Be("Active task");
    }

    [Fact]
    public async Task Handle_ShouldExcludeInactiveTasks()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Active",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            CreatedBy = TestUserId
        });
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Inactive",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = false,
            CreatedBy = TestUserId
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);
        var query = new GetTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.First().Title.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_ShouldOrderByPriorityDescending_ThenByDueDate()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Low priority",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            DueDate = new DateOnly(2025, 1, 1),
            CreatedBy = TestUserId
        });
        seedContext.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "High priority",
            Priority = TaskPriority.High,
            Category = TaskCategory.General,
            IsActive = true,
            DueDate = new DateOnly(2025, 2, 1),
            CreatedBy = TestUserId
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);

        result.Items.First().Title.Should().Be("High priority");
        result.Items.Last().Title.Should().Be("Low priority");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoTasksMatch()
    {
        using var context = _factory.CreateContext();
        var handler = new GetTasksQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private async Task SeedMultipleTasks(int count)
    {
        using var context = _factory.CreateContext();
        for (var i = 0; i < count; i++)
        {
            context.HouseholdTasks.Add(new HouseholdTask
            {
                Title = $"Task {i + 1}",
                Priority = TaskPriority.Medium,
                Category = TaskCategory.General,
                IsActive = true,
                CreatedBy = TestUserId
            });
        }
        await context.SaveChangesAsync();
    }

    private async Task SeedTasksWithCategories()
    {
        using var context = _factory.CreateContext();
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Clean floors",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsActive = true,
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Cook dinner",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cooking,
            IsActive = true,
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Dust shelves",
            Priority = TaskPriority.Low,
            Category = TaskCategory.Cleaning,
            IsActive = true,
            CreatedBy = TestUserId
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedTasksWithPriorities()
    {
        using var context = _factory.CreateContext();
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Urgent fix",
            Priority = TaskPriority.Critical,
            Category = TaskCategory.Maintenance,
            IsActive = true,
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Low prio",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            CreatedBy = TestUserId
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedMixedRecurringTasks()
    {
        using var context = _factory.CreateContext();
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Recurring",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true,
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "One-off",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = false,
            IsActive = true,
            DueDate = new DateOnly(2025, 6, 1),
            CreatedBy = TestUserId
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedTasksWithAssignees()
    {
        using var context = _factory.CreateContext();
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Alice's task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true,
            AssignedToUserId = "user-a",
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Bob's task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true,
            AssignedToUserId = "user-b",
            CreatedBy = TestUserId
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedTasksForSearch()
    {
        using var context = _factory.CreateContext();
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Clean the kitchen",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsActive = true,
            CreatedBy = TestUserId
        });
        context.HouseholdTasks.Add(new HouseholdTask
        {
            Title = "Mow the lawn",
            Priority = TaskPriority.Low,
            Category = TaskCategory.Gardening,
            IsActive = true,
            CreatedBy = TestUserId
        });
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
