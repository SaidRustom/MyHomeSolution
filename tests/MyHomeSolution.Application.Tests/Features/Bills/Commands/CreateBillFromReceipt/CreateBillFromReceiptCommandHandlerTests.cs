using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.CreateBillFromReceipt;

public sealed class CreateBillFromReceiptCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IReceiptAnalysisService _receiptAnalysisService = Substitute.For<IReceiptAnalysisService>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public CreateBillFromReceiptCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);

        _fileStorageService
            .UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"/receipts/{ci.ArgAt<string>(1)}");
    }

    [Fact]
    public async Task Handle_ShouldCreateBillFromAnalyzedReceipt()
    {
        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Costco Wholesale",
            StoreAddress = "123 Main St, Springfield",
            TransactionDate = new DateTimeOffset(2025, 6, 14, 10, 30, 0, TimeSpan.Zero),
            Currency = "USD",
            Subtotal = 42.50m,
            Discount = 2.50m,
            Total = 40.00m,
            Items =
            [
                new ReceiptLineItem { Name = "Organic Chicken Breast", Price = 15.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Whole Milk 1 Gallon", Price = 5.50m, Quantity = 2 },
                new ReceiptLineItem { Name = "Sourdough Bread", Price = 4.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Cheddar Cheese Block", Price = 12.50m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = CreateCommand(category: BillCategory.Groceries);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Costco Wholesale");
        result.Amount.Should().Be(40.00m);
        result.Currency.Should().Be("USD");
        result.Category.Should().Be(BillCategory.Groceries);
        result.BillDate.Should().Be(new DateTimeOffset(2025, 6, 14, 10, 30, 0, TimeSpan.Zero));
        result.ReceiptUrl.Should().NotBeNullOrEmpty();
        result.Notes.Should().Contain("Discount applied: 2.50");
        result.Description.Should().Contain("123 Main St");
        result.Items.Should().HaveCount(4);
        result.Items.Should().Contain(i => i.Name == "Organic Chicken Breast" && i.Price == 15.00m);
        result.Items.Should().Contain(i => i.Name == "Whole Milk 1 Gallon" && i.Quantity == 2 && i.Price == 5.50m);
    }

    [Fact]
    public async Task Handle_ShouldPersistBillToDatabase()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills
            .Include(b => b.Splits)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == result.Id);

        bill.Should().NotBeNull();
        bill!.Title.Should().Be("Test Store");
        bill.Amount.Should().Be(50.00m);
        bill.ReceiptUrl.Should().NotBeNullOrEmpty();
        bill.Items.Should().HaveCount(2);
        bill.Items.Should().Contain(i => i.Name == "Item A" && i.Price == 30.00m);
        bill.Items.Should().Contain(i => i.Name == "Item B" && i.Price == 25.00m);
    }

    [Fact]
    public async Task Handle_ShouldCreateDefaultSplitForCurrentUser_WhenNoSplitsProvided()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Splits.Should().HaveCount(1);
        result.Splits[0].UserId.Should().Be("user-1");
        result.Splits[0].Percentage.Should().Be(100m);
        result.Splits[0].Status.Should().Be(SplitStatus.Paid);
    }

    [Fact]
    public async Task Handle_ShouldCreateSplitsForMultipleUsers()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var command = CreateCommand(splits:
        [
            new BillSplitRequest { UserId = "user-1" },
            new BillSplitRequest { UserId = "user-2" }
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Splits.Should().HaveCount(2);
        result.Splits.Should().AllSatisfy(s => s.Percentage.Should().Be(50m));
        result.Splits.Should().AllSatisfy(s => s.Amount.Should().Be(25m));
        result.Splits.First(s => s.UserId == "user-1").Status.Should().Be(SplitStatus.Paid);
        result.Splits.First(s => s.UserId == "user-2").Status.Should().Be(SplitStatus.Unpaid);
    }

    [Fact]
    public async Task Handle_ShouldUploadReceiptFile()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(), CancellationToken.None);

        await _fileStorageService.Received(1).UploadAsync(
            "receipts",
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishBillCreatedEvent()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillCreatedEvent>(e => e.Title == "Test Store" && e.Amount == 50.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseFallbackDate_WhenAnalysisReturnsDefault()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult() with { TransactionDate = default });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.BillDate.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_ShouldOmitDiscount_WhenZero()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult() with { Discount = 0m });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(CreateCommand(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldCallReceiptAnalysisService()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(), CancellationToken.None);

        await _receiptAnalysisService.Received(1).AnalyzeAsync(
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateUnitPriceForMultiQuantityItems()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult() with
        {
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 7.00m, Quantity = 2 },
                new ReceiptLineItem { Name = "Bread", Price = 3.50m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        var milk = result.Items.First(i => i.Name == "Milk");
        milk.Quantity.Should().Be(2);
        milk.UnitPrice.Should().Be(3.50m);
        milk.Price.Should().Be(7.00m);
        var bread = result.Items.First(i => i.Name == "Bread");
        bread.Quantity.Should().Be(1);
        bread.UnitPrice.Should().Be(3.50m);
        bread.Price.Should().Be(3.50m);
    }

    [Fact]
    public async Task Handle_ShouldDistributeDiscountProportionally()
    {
        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 100m,
            Discount = 10m,
            Total = 90m,
            Items =
            [
                new ReceiptLineItem { Name = "Expensive", Price = 75.00m },
                new ReceiptLineItem { Name = "Cheap", Price = 25.00m }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        var expensive = result.Items.First(i => i.Name == "Expensive");
        var cheap = result.Items.First(i => i.Name == "Cheap");
        expensive.Discount.Should().Be(7.50m);
        cheap.Discount.Should().Be(2.50m);
    }

    [Fact]
    public async Task Handle_ShouldSetZeroDiscount_WhenNoOverallDiscount()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult() with { Discount = 0m });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Items.Should().AllSatisfy(i => i.Discount.Should().Be(0m));
    }

    private CreateBillFromReceiptCommandHandler CreateHandler(Testing.TestDbContext context) =>
        new(context, _currentUserService, _receiptAnalysisService,
            _fileStorageService, _dateTimeProvider, _publisher);

    private static CreateBillFromReceiptCommand CreateCommand(
        BillCategory category = BillCategory.General,
        List<BillSplitRequest>? splits = null) =>
        new()
        {
            FileName = "receipt.jpg",
            ContentType = "image/jpeg",
            Content = new MemoryStream([0xFF, 0xD8, 0xFF]),
            Category = category,
            Splits = splits
        };

    private static ReceiptAnalysisResult CreateDefaultAnalysisResult() => new()
    {
        StoreName = "Test Store",
        StoreAddress = "456 Oak Ave",
        TransactionDate = new DateTimeOffset(2025, 6, 14, 10, 0, 0, TimeSpan.Zero),
        Currency = "USD",
        Subtotal = 55.00m,
        Discount = 5.00m,
        Total = 50.00m,
        Items =
        [
            new ReceiptLineItem { Name = "Item A", Price = 30.00m },
            new ReceiptLineItem { Name = "Item B", Price = 25.00m }
        ]
    };

    private void SetupAnalysisResult(ReceiptAnalysisResult result) =>
        _receiptAnalysisService
            .AnalyzeAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(result);

    public void Dispose() => _factory.Dispose();
}
