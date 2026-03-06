using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Commands.DeleteBill;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.DeleteBill;

public sealed class DeleteBillCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public DeleteBillCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteBill()
    {
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new DeleteBillCommandHandler(context, _currentUserService, _publisher);

        await handler.Handle(new DeleteBillCommand(bill.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var deleted = await assertContext.Bills
            .IgnoreQueryFilters()
            .FirstAsync(b => b.Id == bill.Id);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPublishEventWithAffectedUsers()
    {
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new DeleteBillCommandHandler(context, _currentUserService, _publisher);

        await handler.Handle(new DeleteBillCommand(bill.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillDeletedEvent>(e =>
                e.BillId == bill.Id &&
                e.AffectedUserIds.Contains("user-2") &&
                !e.AffectedUserIds.Contains("user-1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteBillCommandHandler(context, _currentUserService, _publisher);

        var act = () => handler.Handle(
            new DeleteBillCommand(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Bill> SeedBill()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Test Bill",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
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
