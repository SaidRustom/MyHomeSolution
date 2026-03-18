using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTree;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgetTree;

public sealed class GetBudgetTreeQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetTreeQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldReturnTreeWithParentAndChildren()
    {
        await SeedBudgetTreeAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTreeQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTreeQuery(), CancellationToken.None);

        result.Should().HaveCount(1); // One root
        var root = result[0];
        root.Name.Should().Be("Master Budget");
        root.Children.Should().HaveCount(2);
        root.Children.Should().Contain(c => c.Name == "Groceries");
        root.Children.Should().Contain(c => c.Name == "Entertainment");
    }

    [Fact]
    public async Task Handle_ShouldCalculateCurrentPeriodStats()
    {
        await SeedBudgetTreeAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTreeQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTreeQuery(), CancellationToken.None);

        var root = result[0];
        root.CurrentPeriodAllocated.Should().Be(3000m);
        root.CurrentPeriodSpent.Should().Be(1000m);
        root.CurrentPeriodRemaining.Should().Be(2000m);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyWhenNoBudgets()
    {
        using var context = _factory.CreateContext();
        var handler = new GetBudgetTreeQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTreeQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldHandleOrphanedBudgetsAsRoots()
    {
        using var setupContext = _factory.CreateContext();
        var standalone = new Budget
        {
            Name = "Standalone",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.Other,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-1"
        };
        setupContext.Budgets.Add(standalone);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTreeQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTreeQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Standalone");
        result[0].Children.Should().BeEmpty();
    }

    private async Task SeedBudgetTreeAsync()
    {
        using var context = _factory.CreateContext();

        var master = new Budget
        {
            Name = "Master Budget",
            Amount = 3000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        master.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = master.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 3000m,
            CarryoverAmount = 0m
        });

        var groceries = new Budget
        {
            Name = "Groceries",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            ParentBudgetId = master.Id,
            CreatedBy = "user-1"
        };
        groceries.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = groceries.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        });

        var entertainment = new Budget
        {
            Name = "Entertainment",
            Amount = 200m,
            Currency = "CAD",
            Category = BudgetCategory.Entertainment,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            ParentBudgetId = master.Id,
            CreatedBy = "user-1"
        };
        entertainment.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = entertainment.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 200m,
            CarryoverAmount = 0m
        });

        context.Budgets.Add(master);
        context.Budgets.Add(groceries);
        context.Budgets.Add(entertainment);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
