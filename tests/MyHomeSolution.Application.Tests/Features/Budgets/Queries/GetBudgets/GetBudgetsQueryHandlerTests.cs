using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgets;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgets;

public sealed class GetBudgetsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserBudgets()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategory()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            Category = BudgetCategory.Groceries
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Category.Should().Be(BudgetCategory.Groceries);
    }

    [Fact]
    public async Task Handle_ShouldFilterByPeriod()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            Period = BudgetPeriod.Weekly
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            SearchTerm = "Groceries"
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Contain("Groceries");
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsRecurring()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            IsRecurring = true
        }, CancellationToken.None);

        result.Items.Should().AllSatisfy(b => b.IsRecurring.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldIncludeSharedBudgets()
    {
        using var setupContext = _factory.CreateContext();

        var otherBudget = new Budget
        {
            Name = "Shared Budget",
            Amount = 200m,
            Currency = "CAD",
            Category = BudgetCategory.Utilities,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-2"
        };
        setupContext.Budgets.Add(otherBudget);

        var share = new EntityShare
        {
            EntityId = otherBudget.Id,
            EntityType = "Budget",
            SharedWithUserId = "user-1",
            Permission = SharePermission.View
        };
        setupContext.EntityShares.Add(share);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Items.Should().Contain(b => b.Name == "Shared Budget");
    }

    [Fact]
    public async Task Handle_ShouldNotReturnOtherUserBudgets()
    {
        using var setupContext = _factory.CreateContext();
        var otherBudget = new Budget
        {
            Name = "Other User Budget",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-999"
        };
        setupContext.Budgets.Add(otherBudget);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Items.Should().NotContain(b => b.Name == "Other User Budget");
    }

    [Fact]
    public async Task Handle_ShouldSortByName()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            SortBy = "name",
            SortDirection = "asc"
        }, CancellationToken.None);

        result.Items.Should().BeInAscendingOrder(b => b.Name);
    }

    [Fact]
    public async Task Handle_ShouldPaginate()
    {
        await SeedBudgetsAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery
        {
            PageNumber = 1,
            PageSize = 1
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new GetBudgetsQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldCalculateCurrentPeriodSpending()
    {
        using var setupContext = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Tracked Budget",
            Amount = 1000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        var occurrence = new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 1000m,
            CarryoverAmount = 0m
        };
        budget.Occurrences.Add(occurrence);
        setupContext.Budgets.Add(budget);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetsQueryHandler(context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetsQuery(), CancellationToken.None);
        var item = result.Items.First(b => b.Name == "Tracked Budget");
        item.CurrentPeriodSpent.Should().Be(350m);
        item.CurrentPeriodRemaining.Should().Be(650m);
        item.CurrentPeriodPercentUsed.Should().Be(35m);
    }

    private async Task SeedBudgetsAsync()
    {
        using var context = _factory.CreateContext();
        context.Budgets.Add(new Budget
        {
            Name = "Groceries Budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddMonths(-1),
            IsRecurring = true,
            CreatedBy = "user-1"
        });
        context.Budgets.Add(new Budget
        {
            Name = "Entertainment Budget",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.Entertainment,
            Period = BudgetPeriod.Weekly,
            StartDate = Now.AddDays(-3),
            IsRecurring = false,
            CreatedBy = "user-1"
        });
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
