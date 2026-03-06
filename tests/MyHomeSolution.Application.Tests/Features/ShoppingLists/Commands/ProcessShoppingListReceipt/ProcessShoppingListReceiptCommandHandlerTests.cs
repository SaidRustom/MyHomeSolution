using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

public sealed class ProcessShoppingListReceiptCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IReceiptAnalysisService _receiptAnalysisService = Substitute.For<IReceiptAnalysisService>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    private static readonly DateTimeOffset Now = new(2025, 7, 10, 12, 0, 0, TimeSpan.Zero);

    public ProcessShoppingListReceiptCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);

        _fileStorageService
            .UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"/receipts/{ci.ArgAt<string>(1)}");
    }

    [Fact]
    public async Task Handle_ShouldCheckOffMatchingItems_AndAddNewItems()
    {
        var list = await SeedShoppingList("Spaghetti Pasta", "Olive Oil", "Cheese");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Grocery Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 20m,
            Total = 20m,
            Items =
            [
                new ReceiptLineItem { Name = "Spaghetti Pasta", Price = 3.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Olive Oil", Price = 8.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Bread", Price = 4.00m, Quantity = 2 },
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.CheckedItems.Should().HaveCount(2);
        result.CheckedItems.Should().Contain(i => i.Name == "Spaghetti Pasta");
        result.CheckedItems.Should().Contain(i => i.Name == "Olive Oil");

        result.AddedItems.Should().HaveCount(2);
        result.AddedItems.Should().Contain(i => i.Name == "Bread");
        result.AddedItems.Should().Contain(i => i.Name == "Milk");
    }

    [Fact]
    public async Task Handle_ShouldCreateBillLinkedToShoppingList()
    {
        var list = await SeedShoppingList("Pasta");

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.Should().NotBeNull();
        result.Bill.Title.Should().Be("Test Store");
        result.Bill.Amount.Should().Be(50.00m);
        result.Bill.RelatedEntityId.Should().Be(list.Id);
        result.Bill.RelatedEntityType.Should().Be("ShoppingList");
        result.Bill.ReceiptUrl.Should().NotBeNullOrEmpty();

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills
            .Include(b => b.Items)
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == result.BillId);

        bill.Should().NotBeNull();
        bill!.RelatedEntityId.Should().Be(list.Id);
        bill.Items.Should().HaveCount(2);
        bill.Splits.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldPassExistingItemNamesToReceiptAnalysis()
    {
        var list = await SeedShoppingList("Spaghetti Pasta", "Olive Oil");

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        await _receiptAnalysisService.Received(1).AnalyzeAsync(
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Is<IReadOnlyList<string>>(names =>
                names.Count == 2 &&
                names.Contains("Spaghetti Pasta") &&
                names.Contains("Olive Oil")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldToggleExistingItemsAsChecked()
    {
        var list = await SeedShoppingList("Milk", "Bread");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 10m,
            Total = 10m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Bread", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.CheckedItems.Should().HaveCount(2);
        result.CheckedItems.Should().AllSatisfy(i =>
        {
            i.IsChecked.Should().BeTrue();
            i.CheckedAt.Should().Be(Now);
            i.CheckedByUserId.Should().Be("user-1");
        });

        using var assertContext = _factory.CreateContext();
        var items = await assertContext.ShoppingItems
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();
        items.Should().AllSatisfy(i => i.IsChecked.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldAddNewItemsFromBillItems()
    {
        var list = await SeedShoppingList();

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 15m,
            Total = 15m,
            Items =
            [
                new ReceiptLineItem { Name = "Eggs", Price = 6.00m, Quantity = 12 },
                new ReceiptLineItem { Name = "Butter", Price = 9.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.AddedItems.Should().HaveCount(2);
        result.AddedItems.Should().Contain(i => i.Name == "Eggs" && i.Quantity == 12);
        result.AddedItems.Should().Contain(i => i.Name == "Butter" && i.Quantity == 1);
        result.AddedItems.Should().AllSatisfy(i =>
        {
            i.IsChecked.Should().BeFalse();
            i.Notes.Should().Contain("Added from receipt");
        });

        using var assertContext = _factory.CreateContext();
        var items = await assertContext.ShoppingItems
            .Where(i => i.ShoppingListId == list.Id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
        items.Should().HaveCount(2);
        items[0].SortOrder.Should().Be(0);
        items[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldAutoCompleteList_WhenAllItemsChecked()
    {
        var list = await SeedShoppingList("Milk", "Bread");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 10m,
            Total = 10m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Bread", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeTrue();
        updatedList.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_ShouldNotCompleteList_WhenNewItemsAdded()
    {
        var list = await SeedShoppingList("Milk");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 10m,
            Total = 10m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Bread", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeFalse();
        updatedList.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldNotCompleteList_WhenUncheckedItemsRemain()
    {
        var list = await SeedShoppingList("Milk", "Bread", "Cheese");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 5m,
            Total = 5m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldSkipAlreadyCheckedItems()
    {
        using var seedContext = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Milk",
            Quantity = 1,
            SortOrder = 0,
            IsChecked = true,
            CheckedAt = Now.AddHours(-1),
            CheckedByUserId = "user-1"
        });
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 5m,
            Total = 5m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.CheckedItems.Should().BeEmpty();
        result.AddedItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMatchItemsCaseInsensitively()
    {
        var list = await SeedShoppingList("Spaghetti Pasta");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 3m,
            Total = 3m,
            Items =
            [
                new ReceiptLineItem { Name = "spaghetti pasta", Price = 3.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.CheckedItems.Should().HaveCount(1);
        result.CheckedItems[0].Name.Should().Be("Spaghetti Pasta");
        result.AddedItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldDistributeDiscountProportionally()
    {
        var list = await SeedShoppingList();

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
                new ReceiptLineItem { Name = "Expensive", Price = 75.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Cheap", Price = 25.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        var expensive = result.Bill.Items.First(i => i.Name == "Expensive");
        var cheap = result.Bill.Items.First(i => i.Name == "Cheap");
        expensive.Discount.Should().Be(7.50m);
        cheap.Discount.Should().Be(2.50m);
    }

    [Fact]
    public async Task Handle_ShouldCreateDefaultSplitForCurrentUser()
    {
        var list = await SeedShoppingList();

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.Splits.Should().HaveCount(1);
        result.Bill.Splits[0].UserId.Should().Be("user-1");
        result.Bill.Splits[0].Percentage.Should().Be(100m);
        result.Bill.Splits[0].Status.Should().Be(SplitStatus.Paid);
    }

    [Fact]
    public async Task Handle_ShouldUploadReceiptFile()
    {
        var list = await SeedShoppingList();

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

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
        var list = await SeedShoppingList();

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillCreatedEvent>(e => e.Title == "Test Store" && e.Amount == 50.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishShoppingItemCheckedEvents()
    {
        var list = await SeedShoppingList("Milk", "Bread");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 10m,
            Total = 10m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "Bread", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingItemCheckedEvent>(e => e.ItemName == "Milk"),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingItemCheckedEvent>(e => e.ItemName == "Bread"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenShoppingListNotFound()
    {
        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var act = () => handler.Handle(CreateCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldMapGroceryCategoryToBillCategory()
    {
        using var seedContext = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.Category.Should().Be(BillCategory.Groceries);
    }

    [Fact]
    public async Task Handle_ShouldMapHouseholdCategoryToSupplies()
    {
        using var seedContext = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Household",
            Category = ShoppingListCategory.Household,
            CreatedBy = "user-1"
        };
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        SetupAnalysisResult(CreateDefaultAnalysisResult());

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.Category.Should().Be(BillCategory.Supplies);
    }

    [Fact]
    public async Task Handle_ShouldUseFallbackDate_WhenAnalysisReturnsDefault()
    {
        var list = await SeedShoppingList();

        SetupAnalysisResult(CreateDefaultAnalysisResult() with { TransactionDate = default });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.BillDate.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_ShouldAssignCorrectSortOrder_ForNewItems()
    {
        var list = await SeedShoppingList("Existing Item");

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 10m,
            Total = 10m,
            Items =
            [
                new ReceiptLineItem { Name = "New Item A", Price = 5.00m, Quantity = 1 },
                new ReceiptLineItem { Name = "New Item B", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.AddedItems.Should().HaveCount(2);
        result.AddedItems[0].SortOrder.Should().Be(1);
        result.AddedItems[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldNotAddDuplicateFromReceipt_WhenAlreadyCheckedItemExists()
    {
        using var seedContext = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Milk",
            Quantity = 1,
            SortOrder = 0,
            IsChecked = true,
            CheckedAt = Now.AddHours(-2),
            CheckedByUserId = "user-1"
        });
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 5m,
            Total = 5m,
            Items =
            [
                new ReceiptLineItem { Name = "Milk", Price = 5.00m, Quantity = 1 }
            ]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.CheckedItems.Should().BeEmpty();
        result.AddedItems.Should().BeEmpty();

        using var assertContext = _factory.CreateContext();
        var items = await assertContext.ShoppingItems
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldIncludeDiscountNotes_WhenDiscountPresent()
    {
        var list = await SeedShoppingList();

        SetupAnalysisResult(new ReceiptAnalysisResult
        {
            StoreName = "Store",
            TransactionDate = Now,
            Currency = "USD",
            Subtotal = 55m,
            Discount = 5m,
            Total = 50m,
            Items = [new ReceiptLineItem { Name = "Item", Price = 50m }]
        });

        using var context = _factory.CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(CreateCommand(list.Id), CancellationToken.None);

        result.Bill.Notes.Should().Contain("Discount applied: 5.00");
    }

    private ProcessShoppingListReceiptCommandHandler CreateHandler(TestDbContext context) =>
        new(context, _currentUserService, _receiptAnalysisService,
            _fileStorageService, _dateTimeProvider, _publisher);

    private static ProcessShoppingListReceiptCommand CreateCommand(Guid shoppingListId) =>
        new()
        {
            ShoppingListId = shoppingListId,
            FileName = "receipt.jpg",
            ContentType = "image/jpeg",
            Content = new MemoryStream([0xFF, 0xD8, 0xFF])
        };

    private static ReceiptAnalysisResult CreateDefaultAnalysisResult() => new()
    {
        StoreName = "Test Store",
        StoreAddress = "123 Main St",
        TransactionDate = new DateTimeOffset(2025, 7, 9, 10, 0, 0, TimeSpan.Zero),
        Currency = "USD",
        Subtotal = 55m,
        Discount = 5m,
        Total = 50m,
        Items =
        [
            new ReceiptLineItem { Name = "Item A", Price = 30.00m },
            new ReceiptLineItem { Name = "Item B", Price = 25.00m }
        ]
    };

    private async Task<ShoppingList> SeedShoppingList(params string[] itemNames)
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Weekly Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };

        for (var i = 0; i < itemNames.Length; i++)
        {
            list.Items.Add(new ShoppingItem
            {
                ShoppingListId = list.Id,
                Name = itemNames[i],
                Quantity = 1,
                SortOrder = i
            });
        }

        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return list;
    }

    private void SetupAnalysisResult(ReceiptAnalysisResult result) =>
        _receiptAnalysisService
            .AnalyzeAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(result);

    public void Dispose() => _factory.Dispose();
}
