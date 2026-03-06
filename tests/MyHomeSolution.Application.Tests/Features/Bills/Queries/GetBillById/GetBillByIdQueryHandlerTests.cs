using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Queries.GetBillById;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Queries.GetBillById;

public sealed class GetBillByIdQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetBillByIdQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBillWithSplits()
    {
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new GetBillByIdQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillByIdQuery(bill.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(bill.Id);
        result.Title.Should().Be("Groceries");
        result.Amount.Should().Be(100m);
        result.PaidByUserId.Should().Be("user-1");
        result.Splits.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new GetBillByIdQueryHandler(context, _currentUserService, _identityService);

        var act = () => handler.Handle(
            new GetBillByIdQuery(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotInvolved()
    {
        _currentUserService.UserId.Returns("user-3");
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new GetBillByIdQueryHandler(context, _currentUserService, _identityService);

        var act = () => handler.Handle(new GetBillByIdQuery(bill.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowSplitParticipant()
    {
        _currentUserService.UserId.Returns("user-2");
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new GetBillByIdQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillByIdQuery(bill.Id), CancellationToken.None);
        result.Should().NotBeNull();
    }

    private async Task<Bill> SeedBill()
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
        return bill;
    }

    public void Dispose() => _factory.Dispose();
}
