using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.CreateBudget;

public sealed class CreateBudgetCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public CreateBudgetCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldCreateBudgetWithFirstOccurrence()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Groceries",
            Description = "Monthly grocery budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            IsRecurring = true
        };

        var budgetId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var budget = await assertContext.Budgets
            .Include(b => b.Occurrences)
            .FirstAsync(b => b.Id == budgetId);

        budget.Name.Should().Be("Groceries");
        budget.Amount.Should().Be(500m);
        budget.Category.Should().Be(BudgetCategory.Groceries);
        budget.Period.Should().Be(BudgetPeriod.Monthly);
        budget.IsRecurring.Should().BeTrue();
        budget.Occurrences.Should().HaveCount(1);

        var occurrence = budget.Occurrences.First();
        occurrence.AllocatedAmount.Should().Be(500m);
        occurrence.SpentAmount.Should().Be(0m);
        occurrence.PeriodStart.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_ShouldCreateWeeklyBudgetWithCorrectPeriodEnd()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Weekly Entertainment",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.Entertainment,
            Period = BudgetPeriod.Weekly,
            StartDate = Now,
            IsRecurring = false
        };

        var budgetId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occ = await assertContext.BudgetOccurrences
            .FirstAsync(o => o.BudgetId == budgetId);

        occ.PeriodEnd.Should().Be(Now.AddDays(7).AddTicks(-1));
    }

    [Fact]
    public async Task Handle_ShouldCreateAnnualBudgetWithCorrectPeriodEnd()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Annual Insurance",
            Amount = 2400m,
            Currency = "CAD",
            Category = BudgetCategory.Insurance,
            Period = BudgetPeriod.Annually,
            StartDate = Now,
            IsRecurring = true
        };

        var budgetId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occ = await assertContext.BudgetOccurrences
            .FirstAsync(o => o.BudgetId == budgetId);

        occ.PeriodEnd.Should().Be(Now.AddYears(1).AddTicks(-1));
    }

    [Fact]
    public async Task Handle_ShouldLinkToParentBudget()
    {
        // Create parent first
        using var setupContext = _factory.CreateContext();
        var parent = new Budget
        {
            Name = "Master Budget",
            Amount = 3000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            IsRecurring = true
        };
        setupContext.Budgets.Add(parent);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Groceries Sub-budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            IsRecurring = false,
            ParentBudgetId = parent.Id
        };

        var budgetId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var budget = await assertContext.Budgets.FirstAsync(b => b.Id == budgetId);
        budget.ParentBudgetId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenParentBudgetNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Orphan Budget",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now,
            ParentBudgetId = Guid.NewGuid()
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new CreateBudgetCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var command = new CreateBudgetCommand
        {
            Name = "Test",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    public void Dispose() => _factory.Dispose();
}
