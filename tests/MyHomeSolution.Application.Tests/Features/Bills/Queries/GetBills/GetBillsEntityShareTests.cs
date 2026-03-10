using FluentAssertions;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Queries.GetBills;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Queries.GetBills;

public sealed class GetBillsEntityShareTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    public GetBillsEntityShareTests()
    {
        _identityService.GetUserFullNamesByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBills_WhenUserHasEntityShare()
    {
        var billId = await SeedBillWithShare();

        _currentUserService.UserId.Returns("shared-user");

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Id.Should().Be(billId);
    }

    [Fact]
    public async Task Handle_ShouldNotReturnBills_WhenShareIsDeleted()
    {
        await SeedBillWithDeletedShare();

        _currentUserService.UserId.Returns("shared-user");

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldNotReturnDuplicates_WhenUserHasBothSplitAndShare()
    {
        await SeedBillWithSplitAndShare();

        _currentUserService.UserId.Returns("shared-user");

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldNotReturnBills_WhenUserHasNoAccess()
    {
        await SeedBillWithShare();

        _currentUserService.UserId.Returns("no-access-user");

        using var context = _factory.CreateContext();
        var handler = new GetBillsQueryHandler(context, _currentUserService, _identityService);

        var result = await handler.Handle(new GetBillsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    private async Task<Guid> SeedBillWithShare()
    {
        using var context = _factory.CreateContext();

        var bill = new Bill
        {
            Title = "Shared Bill",
            Amount = 200m,
            Currency = "CAD",
            Category = BillCategory.Utilities,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "owner-user",
            CreatedBy = "owner-user"
        };

        var share = new EntityShare
        {
            EntityType = EntityTypes.Bill,
            EntityId = bill.Id,
            SharedWithUserId = "shared-user",
            Permission = SharePermission.View
        };

        context.Bills.Add(bill);
        context.EntityShares.Add(share);
        await context.SaveChangesAsync(CancellationToken.None);

        return bill.Id;
    }

    private async Task SeedBillWithDeletedShare()
    {
        using var context = _factory.CreateContext();

        var bill = new Bill
        {
            Title = "Bill with revoked share",
            Amount = 100m,
            Currency = "CAD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "owner-user",
            CreatedBy = "owner-user"
        };

        var share = new EntityShare
        {
            EntityType = EntityTypes.Bill,
            EntityId = bill.Id,
            SharedWithUserId = "shared-user",
            Permission = SharePermission.View,
            IsDeleted = true
        };

        context.Bills.Add(bill);
        context.EntityShares.Add(share);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SeedBillWithSplitAndShare()
    {
        using var context = _factory.CreateContext();

        var bill = new Bill
        {
            Title = "Double access bill",
            Amount = 300m,
            Currency = "CAD",
            Category = BillCategory.Utilities,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "owner-user",
            CreatedBy = "owner-user"
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id,
            UserId = "shared-user",
            Percentage = 50m,
            Amount = 150m,
            Status = SplitStatus.Unpaid
        });

        var share = new EntityShare
        {
            EntityType = EntityTypes.Bill,
            EntityId = bill.Id,
            SharedWithUserId = "shared-user",
            Permission = SharePermission.Edit
        };

        context.Bills.Add(bill);
        context.EntityShares.Add(share);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
