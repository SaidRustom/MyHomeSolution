using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Queries.GetBills;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Queries.GetBills;

public sealed class GetBillsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetBillsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnBillsForCurrentUser()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategory()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(
            new GetBillsQuery { Category = BillCategory.Groceries },
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Category.Should().Be(BillCategory.Groceries);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(
            new GetBillsQuery { SearchTerm = "Electricity" },
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnBills_WhenUserIsInSplit()
    {
        _currentUserService.UserId.Returns("user-2");
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldPaginate()
    {
        await SeedBills();

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(
            new GetBillsQuery { PageNumber = 1, PageSize = 1 },
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    private async Task SeedBills()
    {
        using var context = _factory.CreateContext();

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

        var bill2 = new Bill
        {
            Title = "Electricity",
            Amount = 200m,
            Currency = "USD",
            Category = BillCategory.Utilities,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill2.Splits.Add(new BillSplit
        {
            BillId = bill2.Id, UserId = "user-1",
            Percentage = 50m, Amount = 100m, Status = SplitStatus.Paid
        });
        bill2.Splits.Add(new BillSplit
        {
            BillId = bill2.Id, UserId = "user-2",
            Percentage = 50m, Amount = 100m, Status = SplitStatus.Unpaid
        });

        context.Bills.AddRange(bill1, bill2);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
