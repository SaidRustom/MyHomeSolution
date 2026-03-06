using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Queries.GetSpendingSummary;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Queries.GetSpendingSummary;

public sealed class GetSpendingSummaryQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetSpendingSummaryQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTotals()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetSpendingSummaryQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetSpendingSummaryQuery(), CancellationToken.None);

        result.TotalSpent.Should().Be(100m);
        result.TotalOwed.Should().Be(50m);
        result.TotalOwing.Should().Be(75m);
        result.NetBalance.Should().Be(-25m);
    }

    [Fact]
    public async Task Handle_ShouldReturnBreakdownByCategory()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetSpendingSummaryQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetSpendingSummaryQuery(), CancellationToken.None);

        result.ByCategory.Should().HaveCount(2);
        result.ByCategory.Should().Contain(c => c.Category == BillCategory.Groceries);
        result.ByCategory.Should().Contain(c => c.Category == BillCategory.Rent);
    }

    [Fact]
    public async Task Handle_ShouldReturnBreakdownByUser()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetSpendingSummaryQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetSpendingSummaryQuery(), CancellationToken.None);

        result.ByUser.Should().HaveCount(1);
        var userSpending = result.ByUser.First();
        userSpending.UserId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetSpendingSummaryQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetSpendingSummaryQuery
            {
                FromDate = DateTimeOffset.UtcNow.AddDays(-2),
                ToDate = DateTimeOffset.UtcNow.AddDays(-0.5)
            },
            CancellationToken.None);

        result.TotalSpent.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeros_WhenNoBills()
    {
        using var context = _factory.CreateContext();
        var handler = new GetSpendingSummaryQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetSpendingSummaryQuery(), CancellationToken.None);

        result.TotalSpent.Should().Be(0);
        result.TotalOwed.Should().Be(0);
        result.TotalOwing.Should().Be(0);
        result.NetBalance.Should().Be(0);
        result.ByCategory.Should().BeEmpty();
        result.ByUser.Should().BeEmpty();
    }

    private async Task SeedBills()
    {
        using var context = _factory.CreateContext();

        // user-1 paid $100, user-2 owes $50
        var bill1 = new Bill
        {
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow.AddDays(-1),
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill1.Splits.Add(new BillSplit
        {
            BillId = bill1.Id, UserId = "user-1",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        bill1.Splits.Add(new BillSplit
        {
            BillId = bill1.Id, UserId = "user-2",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Unpaid
        });

        // user-2 paid $150, user-1 owes $75
        var bill2 = new Bill
        {
            Title = "Rent",
            Amount = 150m,
            Currency = "USD",
            Category = BillCategory.Rent,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-2",
            CreatedBy = "user-2"
        };
        bill2.Splits.Add(new BillSplit
        {
            BillId = bill2.Id, UserId = "user-2",
            Percentage = 50m, Amount = 75m, Status = SplitStatus.Paid
        });
        bill2.Splits.Add(new BillSplit
        {
            BillId = bill2.Id, UserId = "user-1",
            Percentage = 50m, Amount = 75m, Status = SplitStatus.Unpaid
        });

        context.Bills.AddRange(bill1, bill2);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
