using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.UpdateBudget;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.UpdateBudget;

public sealed class UpdateBudgetCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public UpdateBudgetCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUpdateBudgetFields()
    {
        var budget = await SeedBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new UpdateBudgetCommandHandler(context, _currentUserService);

        var command = new UpdateBudgetCommand
        {
            Id = budget.Id,
            Name = "Updated Groceries",
            Description = "Updated description",
            Amount = 600m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Weekly,
            StartDate = budget.StartDate,
            IsRecurring = false
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.Budgets.FirstAsync(b => b.Id == budget.Id);
        updated.Name.Should().Be("Updated Groceries");
        updated.Amount.Should().Be(600m);
        updated.Period.Should().Be(BudgetPeriod.Weekly);
        updated.IsRecurring.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBudgetNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new UpdateBudgetCommandHandler(context, _currentUserService);

        var command = new UpdateBudgetCommand
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenParentNotFound()
    {
        var budget = await SeedBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new UpdateBudgetCommandHandler(context, _currentUserService);

        var command = new UpdateBudgetCommand
        {
            Id = budget.Id,
            Name = "Test",
            Amount = 100m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            ParentBudgetId = Guid.NewGuid()
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Budget> SeedBudgetAsync()
    {
        using var context = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Groceries",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    public void Dispose() => _factory.Dispose();
}
