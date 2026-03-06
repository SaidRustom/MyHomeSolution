using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Queries.GetUserBalances;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Queries.GetUserBalances;

public sealed class GetUserBalancesQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetUserBalancesQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBalancesForUser()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetUserBalancesQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetUserBalancesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var balance = result.First();
        balance.CounterpartyUserId.Should().Be("user-2");
        balance.TotalOwed.Should().Be(50m);
        balance.TotalOwing.Should().Be(0m);
        balance.NetBalance.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_ShouldCalculateNetBalance_WhenBothUsersOweEachOther()
    {
        await SeedBillsBothDirections();

        using var context = _factory.CreateContext();
        var handler = new GetUserBalancesQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetUserBalancesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var balance = result.First();
        balance.TotalOwed.Should().Be(50m);
        balance.TotalOwing.Should().Be(75m);
        balance.NetBalance.Should().Be(-25m);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCounterparty()
    {
        await SeedBillsMultipleCounterparties();

        using var context = _factory.CreateContext();
        var handler = new GetUserBalancesQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(
            new GetUserBalancesQuery { CounterpartyUserId = "user-2" },
            CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().CounterpartyUserId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoBalances()
    {
        using var context = _factory.CreateContext();
        var handler = new GetUserBalancesQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetUserBalancesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldExcludePaidSplits()
    {
        await SeedBillWithPaidSplit();

        using var context = _factory.CreateContext();
        var handler = new GetUserBalancesQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetUserBalancesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private async Task SeedBills()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-1",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-2",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Unpaid
        });
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
    }

    private async Task SeedBillsBothDirections()
    {
        using var context = _factory.CreateContext();

        // user-1 paid, user-2 owes 50
        var bill1 = new Bill
        {
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
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

        // user-2 paid, user-1 owes 75
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

    private async Task SeedBillsMultipleCounterparties()
    {
        using var context = _factory.CreateContext();

        var bill1 = new Bill
        {
            Title = "Bill with user-2",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill1.Splits.Add(new BillSplit
        {
            BillId = bill1.Id, UserId = "user-2",
            Percentage = 100m, Amount = 100m, Status = SplitStatus.Unpaid
        });

        var bill2 = new Bill
        {
            Title = "Bill with user-3",
            Amount = 80m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill2.Splits.Add(new BillSplit
        {
            BillId = bill2.Id, UserId = "user-3",
            Percentage = 100m, Amount = 80m, Status = SplitStatus.Unpaid
        });

        context.Bills.AddRange(bill1, bill2);
        await context.SaveChangesAsync();
    }

    private async Task SeedBillWithPaidSplit()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Paid Bill",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-1",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-2",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
