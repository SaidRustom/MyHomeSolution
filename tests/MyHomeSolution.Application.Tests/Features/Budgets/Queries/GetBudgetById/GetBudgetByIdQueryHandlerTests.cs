using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetById;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgetById;

public sealed class GetBudgetByIdQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetByIdQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
        _identityService.GetUserFullNamesByIdsAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["user-1"] = "Test User" });
    }

    [Fact]
    public async Task Handle_ShouldReturnBudgetDetail()
    {
        var budget = await SeedBudgetWithOccurrenceAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetByIdQuery(budget.Id), CancellationToken.None);

        result.Name.Should().Be("Test Budget");
        result.Amount.Should().Be(500m);
        result.Occurrences.Should().HaveCount(1);
        result.Occurrences[0].AllocatedAmount.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_ShouldIncludeChildBudgets()
    {
        using var setupContext = _factory.CreateContext();
        var parent = new Budget
        {
            Name = "Parent Budget",
            Amount = 3000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-1"
        };
        var child = new Budget
        {
            Name = "Child Budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            ParentBudgetId = parent.Id,
            CreatedBy = "user-1"
        };
        setupContext.Budgets.Add(parent);
        setupContext.Budgets.Add(child);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetByIdQuery(parent.Id), CancellationToken.None);

        result.ChildBudgets.Should().HaveCount(1);
        result.ChildBudgets[0].Name.Should().Be("Child Budget");
    }

    [Fact]
    public async Task Handle_ShouldIncludeLinkedBills()
    {
        using var setupContext = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "With Bills",
            Amount = 1000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-1"
        };
        var bill = new Bill
        {
            Title = "Grocery Store",
            Amount = 50m,
            Currency = "CAD",
            Category = BillCategory.Groceries,
            BillDate = Now,
            PaidByUserId = "user-1"
        };
        budget.BillLinks.Add(new BillBudgetLink
        {
            BillId = bill.Id,
            BudgetId = budget.Id
        });
        setupContext.Bills.Add(bill);
        setupContext.Budgets.Add(budget);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetByIdQuery(budget.Id), CancellationToken.None);

        result.LinkedBills.Should().HaveCount(1);
        result.LinkedBills[0].BillTitle.Should().Be("Grocery Store");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var act = () => handler.Handle(
            new GetBudgetByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotOwnerOrShared()
    {
        _currentUserService.UserId.Returns("user-999");
        var budget = await SeedBudgetWithOccurrenceAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var act = () => handler.Handle(
            new GetBudgetByIdQuery(budget.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowSharedUser()
    {
        _currentUserService.UserId.Returns("user-2");
        var budget = await SeedBudgetWithOccurrenceAsync();

        using var shareContext = _factory.CreateContext();
        shareContext.EntityShares.Add(new EntityShare
        {
            EntityId = budget.Id,
            EntityType = "Budget",
            SharedWithUserId = "user-2",
            Permission = SharePermission.View
        });
        await shareContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetByIdQueryHandler(
            context, _currentUserService, _identityService, _dateTimeProvider);

        var result = await handler.Handle(
            new GetBudgetByIdQuery(budget.Id), CancellationToken.None);

        result.Name.Should().Be("Test Budget");
    }

    private async Task<Budget> SeedBudgetWithOccurrenceAsync()
    {
        using var context = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Test Budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            CreatedBy = "user-1"
        };
        budget.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = Now,
            PeriodEnd = Now.AddMonths(1),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        });
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    public void Dispose() => _factory.Dispose();
}
