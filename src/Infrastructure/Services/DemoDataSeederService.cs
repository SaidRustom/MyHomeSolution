using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Identity;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Seeds realistic demo data for a newly registered demo user.
/// Creates fake friend accounts, connections, tasks, bills, budgets,
/// shopping lists, and entity shares covering ±1 month from creation.
/// </summary>
public sealed class DemoDataSeederService(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoDataSeederService> logger)
{
    /// <summary>Deterministic password used for all fake friend accounts.</summary>
    private const string FakeFriendPassword = "Demo@Friend123!";

    private static readonly (string First, string Last, string Email)[] FakeFriends =
    [
        ("Alex", "Chen", "alex.chen.demo@myhome.local"),
        ("Maria", "Rodriguez", "maria.rodriguez.demo@myhome.local"),
        ("James", "Patel", "james.patel.demo@myhome.local"),
    ];

    public async Task SeedAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await seedAsync(userId, cancellationToken);
        }
        catch(Exception ex)
        {

        }
    }

    private async Task seedAsync(string userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting demo data seeding for user {UserId}", userId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var occurrenceScheduler = scope.ServiceProvider.GetRequiredService<IOccurrenceScheduler>();

        var now = dateTimeProvider.UtcNow;
        var today = dateTimeProvider.Today;
        var monthAgo = now.AddMonths(-1);
        var monthFromNow = now.AddMonths(1);

        // ── Create fake friend accounts ─────────────────────────────────
        var friendIds = new List<string>();
        foreach (var (first, last, email) in FakeFriends)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                friendIds.Add(existing.Id);
                continue;
            }

            var friend = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = first,
                LastName = last,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = monthAgo.AddDays(-1)
            };

            var createResult = await userManager.CreateAsync(friend, FakeFriendPassword);
            if (createResult.Succeeded)
            {
                friendIds.Add(friend.Id);
                logger.LogInformation("Created demo friend: {Name} ({Email})", friend.FullName, email);
            }
            else
            {
                logger.LogWarning("Failed to create demo friend {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // ── Create accepted connections ─────────────────────────────────
        foreach (var friendId in friendIds)
        {
            var connection = new UserConnection
            {
                RequesterId = userId,
                AddresseeId = friendId,
                Status = ConnectionStatus.Accepted,
                RespondedAt = monthAgo.AddDays(Random.Shared.Next(1, 5))
            };
            db.UserConnections.Add(connection);
        }

        // ── Seed Tasks ──────────────────────────────────────────────────
        var tasks = CreateDemoTasks(userId, friendIds, today, monthAgo);

        foreach (var task in tasks)
            task.CreatedBy = userId;

        db.HouseholdTasks.AddRange(tasks);

        // ── Seed Budgets ────────────────────────────────────────────────
        var budgets = CreateDemoBudgets(userId, monthAgo, monthFromNow);

        foreach (var budget in budgets)
            budget.CreatedBy = userId;

        db.Budgets.AddRange(budgets);

        // ── Seed Shopping Lists ─────────────────────────────────────────
        var shoppingLists = CreateDemoShoppingLists(userId, today, monthAgo);

        foreach (var shoppingList in shoppingLists)
            shoppingList.CreatedBy = userId;

        db.ShoppingLists.AddRange(shoppingLists);

        // ── Seed Bills ──────────────────────────────────────────────────
        var bills = CreateDemoBills(userId, friendIds, monthAgo, now, budgets);

        foreach (var bill in bills)
            bill.CreatedBy = userId;

        db.Bills.AddRange(bills);

        // ── Save everything to get IDs ──────────────────────────────────
        await db.SaveSeederChangesAsync(userId, cancellationToken);

        foreach(var task in tasks.Where(x => x.RecurrencePattern != null && x.IsRecurring))
        {
            await occurrenceScheduler.SyncOccurrencesAsync(task.Id, cancellationToken);
        }

        // ── Create entity shares with friends ───────────────────────────
        var shares = CreateDemoShares(userId, friendIds, tasks, shoppingLists, budgets);

        foreach (var share in shares)
            share.CreatedBy = userId;

        db.EntityShares.AddRange(shares);

        // ── Create some notifications ───────────────────────────────────
        var notifications = CreateDemoNotifications(userId, friendIds, tasks, bills, now);
        db.Notifications.AddRange(notifications);

        await db.SaveSeederChangesAsync(userId, cancellationToken);

        logger.LogInformation(
            "Demo data seeding complete for user {UserId}: {Tasks} tasks, {Bills} bills, {Budgets} budgets, {Lists} shopping lists, {Shares} shares",
            userId, tasks.Count, bills.Count, budgets.Count, shoppingLists.Count, shares.Count);
    }

    // ════════════════════════════════════════════════════════════════════
    // Tasks
    // ════════════════════════════════════════════════════════════════════

    private static List<HouseholdTask> CreateDemoTasks(
        string userId, List<string> friendIds, DateOnly today, DateTimeOffset monthAgo)
    {
        var firstFriend = friendIds.Count > 0 ? friendIds[0] : null;
        var secondFriend = friendIds.Count > 1 ? friendIds[1] : null;

        var tasks = new List<HouseholdTask>
        {
            CreateTask("Vacuum living room & hallway", "Vacuum all carpeted areas and mop the hallway",
                TaskPriority.Medium, TaskCategory.Cleaning, 45, userId, today.AddDays(-21),
                isRecurring: true, RecurrenceType.Weekly, friendIds),

            CreateTask("Take out recycling", "Sort recycling bins and bring to curb by 7 AM",
                TaskPriority.Low, TaskCategory.General, 15, userId, today.AddDays(-25),
                isRecurring: true, RecurrenceType.Weekly, []),

            CreateTask("Grocery run — weekly essentials", "Milk, eggs, bread, vegetables, fruits, chicken",
                TaskPriority.High, TaskCategory.Shopping, 60, firstFriend, today.AddDays(-18),
                isRecurring: true, RecurrenceType.Weekly, friendIds),

            CreateTask("Clean bathroom", "Scrub tiles, clean toilet, wipe mirrors, replace towels",
                TaskPriority.Medium, TaskCategory.Cleaning, 40, secondFriend ?? userId, today.AddDays(-20),
                isRecurring: true, RecurrenceType.Weekly, friendIds),

            CreateTask("Water indoor plants", "Water all potted plants and check soil moisture",
                TaskPriority.Low, TaskCategory.Gardening, 15, userId, today.AddDays(-28),
                isRecurring: true, RecurrenceType.Daily, []),

            CreateTask("Fix leaky kitchen faucet", "Replace washer and tighten fittings",
                TaskPriority.High, TaskCategory.Maintenance, 90, userId, today.AddDays(3)),

            CreateTask("Meal prep for the week", "Cook chicken, rice, chop vegetables for lunches",
                TaskPriority.Medium, TaskCategory.Cooking, 120, firstFriend ?? userId, today.AddDays(-14),
                isRecurring: true, RecurrenceType.Weekly, friendIds),

            CreateTask("Organize garage", "Sort tools, donate unused items, sweep floor",
                TaskPriority.Low, TaskCategory.Organization, 180, userId, today.AddDays(7)),

            CreateTask("Do laundry — darks", "Wash, dry, and fold dark clothing",
                TaskPriority.Medium, TaskCategory.Laundry, 30, userId, today.AddDays(-16),
                isRecurring: true, RecurrenceType.Weekly, []),

            CreateTask("Feed and walk the dog", "Morning and evening walks, fill food and water bowls",
                TaskPriority.Critical, TaskCategory.PetCare, 45, userId, today.AddDays(-30),
                isRecurring: true, RecurrenceType.Daily, friendIds),

            CreateTask("Change bed sheets", "Strip beds, wash sheets, and make beds with fresh linens",
                TaskPriority.Low, TaskCategory.Laundry, 30, secondFriend ?? userId, today.AddDays(-13),
                isRecurring: true, RecurrenceType.Monthly, []),

            CreateTask("Mow the lawn", "Front and back yard, edge walkways",
                TaskPriority.Medium, TaskCategory.Gardening, 60, userId, today.AddDays(-10),
                isRecurring: true, RecurrenceType.Weekly, friendIds),
        };

        return tasks;
    }

    private static HouseholdTask CreateTask(
        string title, string description, TaskPriority priority, TaskCategory category,
        int durationMinutes, string? assignedTo, DateOnly dueDate,
        bool isRecurring = false, RecurrenceType recurrenceType = RecurrenceType.Weekly,
        List<string>? assignees = null)
    {
        var task = new HouseholdTask
        {
            CreatedBy = assignedTo,
            Title = title,
            Description = description,
            Priority = priority,
            Category = category,
            EstimatedDurationMinutes = durationMinutes,
            IsRecurring = isRecurring,
            IsActive = true,
            DueDate = dueDate,
            AssignedToUserId = assignedTo
        };

        if (isRecurring)
        {
            var pattern = new RecurrencePattern
            {
                HouseholdTaskId = task.Id,
                Type = recurrenceType,
                Interval = 1,
                StartDate = dueDate,
                EndDate = null,
                LastAssigneeIndex = -1
            };

            if (assignees is { Count: > 0 })
            {
                for (var i = 0; i < assignees.Count; i++)
                {
                    pattern.Assignees.Add(new RecurrenceAssignee
                    {
                        RecurrencePatternId = pattern.Id,
                        UserId = assignees[i],
                        Order = i
                    });
                }
            }

            task.RecurrencePattern = pattern;
        }

        return task;
    }

    // ════════════════════════════════════════════════════════════════════
    // Budgets
    // ════════════════════════════════════════════════════════════════════

    private static List<Budget> CreateDemoBudgets(
        string userId, DateTimeOffset monthAgo, DateTimeOffset monthFromNow)
    {
        var budgets = new List<Budget>
        {
            CreateBudget("Monthly Groceries", "Weekly grocery and food spending",
                850m, BudgetCategory.Groceries, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Utilities", "Electricity, water, gas",
                280m, BudgetCategory.Utilities, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Dining Out", "Restaurants, takeout, coffee shops",
                200m, BudgetCategory.DiningOut, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Entertainment", "Streaming, movies, games, outings",
                150m, BudgetCategory.Entertainment, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Transportation", "Gas, transit pass, parking",
                250m, BudgetCategory.Transportation, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Healthcare", "Prescriptions, dental, physio",
                120m, BudgetCategory.Healthcare, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Home Improvement", "Tools, paint, small repairs",
                300m, BudgetCategory.HomeImprovement, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Savings Fund", "Emergency and rainy day savings",
                500m, BudgetCategory.Savings, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Subscriptions", "Netflix, Spotify, gym membership",
                85m, BudgetCategory.Subscriptions, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),

            CreateBudget("Pet Care", "Dog food, vet visits, grooming",
                175m, BudgetCategory.Pets, BudgetPeriod.Monthly, monthAgo, monthFromNow, true),
        };

        return budgets;
    }

    private static Budget CreateBudget(
        string name, string description, decimal amount,
        BudgetCategory category, BudgetPeriod period,
        DateTimeOffset startDate, DateTimeOffset endDate, bool isRecurring)
    {
        var budget = new Budget
        {
            Name = name,
            Description = description,
            Amount = amount,
            Currency = "CAD",
            Category = category,
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            IsRecurring = isRecurring
        };

        // Create occurrences for each period
        var periodStart = startDate;
        while (periodStart < endDate)
        {
            var periodEnd = period switch
            {
                BudgetPeriod.Weekly => periodStart.AddDays(7),
                BudgetPeriod.Monthly => periodStart.AddMonths(1),
                BudgetPeriod.Annually => periodStart.AddYears(1),
                _ => periodStart.AddMonths(1)
            };

            if (periodEnd > endDate)
                periodEnd = endDate;

            budget.Occurrences.Add(new BudgetOccurrence
            {
                BudgetId = budget.Id,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                AllocatedAmount = amount,
                CarryoverAmount = 0m
            });

            periodStart = periodEnd;
        }

        return budget;
    }

    // ════════════════════════════════════════════════════════════════════
    // Shopping Lists
    // ════════════════════════════════════════════════════════════════════

    private static List<ShoppingList> CreateDemoShoppingLists(
        string userId, DateOnly today, DateTimeOffset monthAgo)
    {
        return
        [
            CreateShoppingList("Weekly Groceries", "Regular weekly grocery run",
                ShoppingListCategory.Groceries, today.AddDays(2), false,
                [
                    ("Whole milk (2L)", 1, "L"), ("Free-range eggs (dozen)", 1, "dozen"),
                    ("Sourdough bread", 1, "loaf"), ("Chicken breast (1kg)", 1, "kg"),
                    ("Broccoli", 2, "head"), ("Baby spinach (200g)", 1, "bag"),
                    ("Cheddar cheese (400g)", 1, "block"), ("Greek yogurt", 2, "tub"),
                    ("Bananas", 1, "bunch"), ("Tomatoes", 4, "pcs"),
                    ("Olive oil", 1, "bottle"), ("Basmati rice (2kg)", 1, "bag"),
                ]),

            CreateShoppingList("Household Supplies", "Monthly cleaning and home supplies",
                ShoppingListCategory.Household, today.AddDays(5), false,
                [
                    ("Dish soap", 1, "bottle"), ("Laundry detergent", 1, "jug"),
                    ("Paper towels", 2, "roll"), ("Trash bags (50ct)", 1, "box"),
                    ("All-purpose cleaner", 1, "spray"), ("Sponges (3-pack)", 1, "pack"),
                ]),

            CreateShoppingList("Last Week's Groceries", "Completed grocery run",
                ShoppingListCategory.Groceries, today.AddDays(-7), true,
                [
                    ("Salmon fillets", 2, "pcs"), ("Sweet potatoes", 3, "pcs"),
                    ("Avocados", 4, "pcs"), ("Almond milk", 1, "carton"),
                    ("Pasta (spaghetti)", 2, "box"), ("Marinara sauce", 1, "jar"),
                    ("Garlic (head)", 2, "pcs"), ("Onions (yellow)", 3, "pcs"),
                    ("Bell peppers", 3, "pcs"), ("Ground beef (500g)", 1, "pack"),
                ]),

            CreateShoppingList("Pet Supplies", "Monthly dog supplies",
                ShoppingListCategory.General, today.AddDays(10), false,
                [
                    ("Premium dog food (15kg)", 1, "bag"), ("Dog treats", 2, "bag"),
                    ("Poop bags (120ct)", 1, "roll"), ("Chew toy", 1, "pcs"),
                ]),

            CreateShoppingList("Party Supplies — Alex's Birthday", "Birthday party this weekend",
                ShoppingListCategory.Other, today.AddDays(12), false,
                [
                    ("Birthday cake (chocolate)", 1, "pcs"), ("Balloons (assorted)", 1, "pack"),
                    ("Paper plates (20ct)", 1, "pack"), ("Napkins", 1, "pack"),
                    ("Candles", 1, "box"), ("Chips & dip", 3, "bag"),
                    ("Soda (2L bottles)", 4, "bottle"), ("Ice cream (vanilla)", 1, "tub"),
                ]),

            CreateShoppingList("Health & Personal Care", "Monthly personal care restock",
                ShoppingListCategory.Personal, today.AddDays(-3), true,
                [
                    ("Toothpaste", 1, "tube"), ("Shampoo", 1, "bottle"),
                    ("Body wash", 1, "bottle"), ("Deodorant", 1, "stick"),
                    ("Bandages (box)", 1, "box"), ("Vitamin D supplements", 1, "bottle"),
                ]),
        ];
    }

    private static ShoppingList CreateShoppingList(
        string title, string description, ShoppingListCategory category,
        DateOnly dueDate, bool isCompleted,
        (string Name, int Qty, string Unit)[] items)
    {
        var list = new ShoppingList
        {
            Title = title,
            Description = description,
            Category = category,
            DueDate = dueDate,
            IsCompleted = isCompleted,
            CompletedAt = isCompleted
                ? new DateTimeOffset(dueDate.ToDateTime(TimeOnly.FromTimeSpan(
                    TimeSpan.FromHours(Random.Shared.Next(10, 18)))), TimeSpan.Zero)
                : null
        };

        for (var i = 0; i < items.Length; i++)
        {
            var (name, qty, unit) = items[i];
            list.Items.Add(new ShoppingItem
            {
                ShoppingListId = list.Id,
                Name = name,
                Quantity = qty,
                Unit = unit,
                SortOrder = i,
                IsChecked = isCompleted || (Random.Shared.NextDouble() > 0.6),
                CheckedAt = isCompleted
                    ? new DateTimeOffset(dueDate.ToDateTime(TimeOnly.FromTimeSpan(
                        TimeSpan.FromHours(Random.Shared.Next(10, 18)))), TimeSpan.Zero)
                    : null
            });
        }

        return list;
    }

    // ════════════════════════════════════════════════════════════════════
    // Bills
    // ════════════════════════════════════════════════════════════════════

    private static List<Bill> CreateDemoBills(
        string userId, List<string> friendIds, DateTimeOffset monthAgo,
        DateTimeOffset now, List<Budget> budgets)
    {
        var bills = new List<Bill>();

        // Past bills with realistic dates scattered over the last month
        bills.Add(CreateBill("Metro Groceries — Feb 28",
            "Weekly groceries from Metro", 127.43m, BillCategory.Groceries,
            monthAgo.AddDays(2), userId, friendIds,
            [("Chicken breast 1kg", 1, 12.99m), ("Milk 2L", 2, 5.49m), ("Eggs dozen", 1, 4.99m),
             ("Bread", 2, 3.49m), ("Vegetables assorted", 1, 18.75m), ("Fruits", 1, 15.60m),
             ("Cheese block", 1, 8.99m), ("Yogurt 4-pack", 2, 6.49m), ("Rice 2kg", 1, 7.99m),
             ("Pasta", 3, 2.19m), ("Olive oil", 1, 11.49m), ("Snacks", 2, 4.25m)],
            FindBudget(budgets, BudgetCategory.Groceries)));

        bills.Add(CreateBill("Hydro-Québec — March",
            "Monthly electricity bill", 94.50m, BillCategory.Utilities,
            monthAgo.AddDays(5), userId, [],
            [("Electricity usage", 1, 78.50m), ("Delivery charge", 1, 16.00m)],
            FindBudget(budgets, BudgetCategory.Utilities)));

        bills.Add(CreateBill("Enbridge Gas — March",
            "Monthly gas bill", 68.20m, BillCategory.Utilities,
            monthAgo.AddDays(7), userId, [],
            [("Gas usage", 1, 52.20m), ("Customer charge", 1, 16.00m)],
            FindBudget(budgets, BudgetCategory.Utilities)));

        bills.Add(CreateBill("Pizza Night — Domino's",
            "Friday pizza with friends", 52.75m, BillCategory.General,
            monthAgo.AddDays(10), userId, friendIds,
            [("Large pepperoni", 1, 18.99m), ("Medium veggie", 1, 15.99m),
             ("Garlic bread", 1, 6.99m), ("2L Coke", 2, 3.49m), ("Dipping sauces", 2, 1.50m)],
            FindBudget(budgets, BudgetCategory.DiningOut)));

        bills.Add(CreateBill("Costco Household Run",
            "Monthly Costco bulk buy", 215.88m, BillCategory.Supplies,
            monthAgo.AddDays(12), userId, friendIds,
            [("Paper towels 12-pack", 1, 22.99m), ("Laundry detergent", 1, 18.99m),
             ("Trash bags", 1, 14.99m), ("Dish soap 3-pack", 1, 12.99m),
             ("Chicken thighs 3kg", 1, 24.99m), ("Ground beef 2kg", 1, 19.99m),
             ("Frozen vegetables", 2, 11.99m), ("Cereal variety", 1, 13.99m),
             ("Coffee beans 1kg", 1, 16.99m), ("Butter 2-pack", 1, 9.99m),
             ("Toilet paper 24-pack", 1, 19.99m), ("Snack bars", 1, 12.99m),
             ("Bottled water 24-pack", 1, 8.99m)],
            FindBudget(budgets, BudgetCategory.Groceries)));

        bills.Add(CreateBill("Netflix + Spotify",
            "Monthly streaming subscriptions", 33.97m, BillCategory.Other,
            monthAgo.AddDays(15), userId, [],
            [("Netflix Standard", 1, 16.49m), ("Spotify Premium", 1, 11.99m),
             ("Tax", 1, 5.49m)],
            FindBudget(budgets, BudgetCategory.Subscriptions)));

        bills.Add(CreateBill("Shell Gas Station",
            "Filled up the car", 78.45m, BillCategory.Other,
            monthAgo.AddDays(18), userId, [],
            [("Regular unleaded 52L", 1, 78.45m)],
            FindBudget(budgets, BudgetCategory.Transportation)));

        bills.Add(CreateBill("Vet Visit — Annual Checkup",
            "Dog annual checkup and vaccinations", 285.00m, BillCategory.Other,
            monthAgo.AddDays(20), userId, [],
            [("Annual exam", 1, 95.00m), ("Rabies vaccine", 1, 45.00m),
             ("DHPP vaccine", 1, 55.00m), ("Heartworm test", 1, 50.00m),
             ("Flea/tick prevention 3mo", 1, 40.00m)],
            FindBudget(budgets, BudgetCategory.Pets)));

        bills.Add(CreateBill("Home Depot — Faucet Parts",
            "Parts for kitchen faucet repair", 42.67m, BillCategory.Maintenance,
            monthAgo.AddDays(22), userId, [],
            [("Faucet washer kit", 1, 12.99m), ("Plumber's tape", 2, 4.99m),
             ("Adjustable wrench", 1, 18.99m)],
            FindBudget(budgets, BudgetCategory.HomeImprovement)));

        bills.Add(CreateBill("Loblaws Groceries — Mar 14",
            "Mid-week grocery top-up", 68.32m, BillCategory.Groceries,
            monthAgo.AddDays(24), userId, friendIds,
            [("Salmon fillet", 1, 14.99m), ("Avocados 3-pack", 1, 5.99m),
             ("Cherry tomatoes", 1, 4.49m), ("Fresh herbs", 2, 3.99m),
             ("Lemons", 4, 0.79m), ("Hummus", 1, 4.99m),
             ("Pita bread", 1, 3.99m), ("Deli turkey", 1, 8.99m),
             ("Apple juice", 1, 4.49m), ("Granola bars", 1, 5.99m)],
            FindBudget(budgets, BudgetCategory.Groceries)));

        bills.Add(CreateBill("Movie Night — Cineplex",
            "Weekend movie outing", 48.50m, BillCategory.Other,
            now.AddDays(-5), userId, friendIds,
            [("Adult ticket", 3, 14.50m), ("Large popcorn combo", 1, 16.99m)],
            FindBudget(budgets, BudgetCategory.Entertainment)));

        bills.Add(CreateBill("Shoppers Drug Mart",
            "Personal care and pharmacy", 56.82m, BillCategory.Other,
            now.AddDays(-3), userId, [],
            [("Toothpaste", 1, 6.49m), ("Shampoo", 1, 12.99m),
             ("Vitamin D 1000IU", 1, 14.99m), ("Bandages", 1, 7.49m),
             ("Ibuprofen", 1, 9.99m)],
            FindBudget(budgets, BudgetCategory.Healthcare)));

        bills.Add(CreateBill("Uber Eats — Thai Express",
            "Lunch delivery", 38.45m, BillCategory.General,
            now.AddDays(-1), userId, friendIds,
            [("Pad Thai", 1, 16.99m), ("Green curry", 1, 15.99m),
             ("Delivery fee", 1, 3.99m), ("Tip", 1, 3.00m)],
            FindBudget(budgets, BudgetCategory.DiningOut)));

        return bills;
    }

    private static Bill CreateBill(
        string title, string description, decimal amount, BillCategory category,
        DateTimeOffset billDate, string paidByUserId, List<string> splitWith,
        (string Name, int Qty, decimal Price)[] items,
        Budget? linkedBudget)
    {
        var bill = new Bill
        {
            Title = title,
            Description = description,
            Amount = amount,
            Currency = "CAD",
            Category = category,
            BillDate = billDate,
            PaidByUserId = paidByUserId
        };

        foreach (var (name, qty, price) in items)
        {
            bill.Items.Add(new BillItem
            {
                BillId = bill.Id,
                Name = name,
                Quantity = qty,
                UnitPrice = price,
                Price = qty * price
            });
        }

        // Create splits
        var allUsers = new List<string> { paidByUserId };
        allUsers.AddRange(splitWith);
        var splitPercent = 100m / allUsers.Count;
        var splitAmount = Math.Round(amount / allUsers.Count, 2);

        foreach (var uid in allUsers)
        {
            bill.Splits.Add(new BillSplit
            {
                BillId = bill.Id,
                UserId = uid,
                Percentage = splitPercent,
                Amount = splitAmount,
                Status = SplitStatus.Paid,
                PaidAt = uid == paidByUserId ? billDate : null,
                OwedToUserId = uid != paidByUserId ? paidByUserId : null
            });
        }

        // Link to budget occurrence
        if (linkedBudget is not null)
        {
            var matchingOcc = linkedBudget.Occurrences
                .FirstOrDefault(o => o.PeriodStart <= billDate && o.PeriodEnd > billDate)
                ?? linkedBudget.Occurrences.LastOrDefault();

            if (matchingOcc is not null)
            {
                bill.BudgetLink = new BillBudgetLink
                {
                    BillId = bill.Id,
                    BudgetId = linkedBudget.Id,
                    BudgetOccurrenceId = matchingOcc.Id
                };
            }
        }

        return bill;
    }

    private static Budget? FindBudget(List<Budget> budgets, BudgetCategory category) =>
        budgets.FirstOrDefault(b => b.Category == category);

    // ════════════════════════════════════════════════════════════════════
    // Entity Shares
    // ════════════════════════════════════════════════════════════════════

    private static List<EntityShare> CreateDemoShares(
        string userId, List<string> friendIds,
        List<HouseholdTask> tasks, List<ShoppingList> shoppingLists,
        List<Budget> budgets)
    {
        var shares = new List<EntityShare>();

        if (friendIds.Count == 0) return shares;

        // Share some tasks with friends
        foreach (var task in tasks.Take(5))
        {
            var friendId = friendIds[Random.Shared.Next(friendIds.Count)];
            shares.Add(new EntityShare
            {
                EntityId = task.Id,
                EntityType = EntityTypes.HouseholdTask,
                SharedWithUserId = friendId,
                Permission = SharePermission.Edit
            });
        }

        // Share shopping lists
        foreach (var list in shoppingLists.Take(3))
        {
            foreach (var friendId in friendIds.Take(2))
            {
                shares.Add(new EntityShare
                {
                    EntityId = list.Id,
                    EntityType = EntityTypes.ShoppingList,
                    SharedWithUserId = friendId,
                    Permission = SharePermission.Edit
                });
            }
        }

        // Share budgets (view only)
        foreach (var budget in budgets.Take(4))
        {
            var friendId = friendIds[0];
            shares.Add(new EntityShare
            {
                EntityId = budget.Id,
                EntityType = EntityTypes.Budget,
                SharedWithUserId = friendId,
                Permission = SharePermission.View
            });
        }

        return shares;
    }

    // ════════════════════════════════════════════════════════════════════
    // Notifications
    // ════════════════════════════════════════════════════════════════════

    private static List<Notification> CreateDemoNotifications(
        string userId, List<string> friendIds,
        List<HouseholdTask> tasks, List<Bill> bills, DateTimeOffset now)
    {
        var notifications = new List<Notification>();
        if (friendIds.Count == 0) return notifications;

        notifications.Add(new Notification
        {
            Title = "Alex Chen accepted your connection request",
            Description = "You are now connected with Alex Chen. You can share tasks, bills, and shopping lists.",
            Type = NotificationType.ConnectionRequestAccepted,
            FromUserId = friendIds[0],
            ToUserId = userId,
            IsRead = true,
            ReadAt = now.AddDays(-20)
        });

        if (friendIds.Count > 1)
        {
            notifications.Add(new Notification
            {
                Title = "Maria Rodriguez accepted your connection request",
                Description = "You are now connected with Maria Rodriguez.",
                Type = NotificationType.ConnectionRequestAccepted,
                FromUserId = friendIds[1],
                ToUserId = userId,
                IsRead = true,
                ReadAt = now.AddDays(-18)
            });
        }

        // Bill shared notification
        if (bills.Count > 0)
        {
            notifications.Add(new Notification
            {
                Title = "New bill: Metro Groceries",
                Description = "A new grocery bill of $127.43 has been created and split with you.",
                Type = NotificationType.BillCreated,
                FromUserId = userId,
                ToUserId = friendIds[0],
                RelatedEntityId = bills[0].Id,
                RelatedEntityType = EntityTypes.Bill,
                IsRead = false
            });
        }

        // Task assigned notification
        if (tasks.Count > 2)
        {
            notifications.Add(new Notification
            {
                Title = "Task assigned: Grocery run — weekly essentials",
                Description = "You have been assigned to the weekly grocery run task.",
                Type = NotificationType.TaskAssigned,
                FromUserId = userId,
                ToUserId = friendIds[0],
                RelatedEntityId = tasks[2].Id,
                RelatedEntityType = EntityTypes.HouseholdTask,
                IsRead = false
            });
        }

        // Recent unread notifications for the demo user
        notifications.Add(new Notification
        {
            Title = "Shopping list shared with you",
            Description = "Alex Chen shared 'Weekly Groceries' shopping list with you.",
            Type = NotificationType.ShareReceived,
            FromUserId = friendIds[0],
            ToUserId = userId,
            IsRead = false
        });

        notifications.Add(new Notification
        {
            Title = "Task completed: Vacuum living room",
            Description = "The vacuum task occurrence has been marked as completed.",
            Type = NotificationType.OccurrenceCompleted,
            FromUserId = userId,
            ToUserId = userId,
            RelatedEntityId = tasks.Count > 0 ? tasks[0].Id : null,
            RelatedEntityType = EntityTypes.HouseholdTask,
            IsRead = false
        });

        return notifications;
    }

    // ════════════════════════════════════════════════════════════════════
    // Cleanup
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Purges ALL data for the given demo user including fake friends
    /// that are not connected to any other non-demo user.
    /// </summary>
    public async Task PurgeUserDataAsync(string userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Purging demo data for user {UserId}", userId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Delete in dependency order to avoid FK violations

        // 1. Notifications
        var notifications = await db.Notifications
            .Where(n => n.ToUserId == userId || n.FromUserId == userId)
            .ToListAsync(cancellationToken);
        db.Notifications.RemoveRange(notifications);

        // 2. Entity shares
        var shares = await db.EntityShares
            .Where(s => s.CreatedBy == userId || s.SharedWithUserId == userId)
            .ToListAsync(cancellationToken);
        db.EntityShares.RemoveRange(shares);

        // 3. Bill budget links (must come before bills and budget occurrences)
        var userBillIds = await db.Bills
            .Where(b => b.CreatedBy == userId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        var billLinks = await db.BillBudgetLinks
            .Where(l => userBillIds.Contains(l.BillId))
            .ToListAsync(cancellationToken);
        db.BillBudgetLinks.RemoveRange(billLinks);

        // 4. Bill items, splits, related items
        var billItems = await db.BillItems
            .Where(i => userBillIds.Contains(i.BillId))
            .ToListAsync(cancellationToken);
        db.BillItems.RemoveRange(billItems);

        var billSplits = await db.BillSplits
            .Where(s => userBillIds.Contains(s.BillId))
            .ToListAsync(cancellationToken);
        db.BillSplits.RemoveRange(billSplits);

        var billRelated = await db.BillRelatedItems
            .Where(r => userBillIds.Contains(r.BillId))
            .ToListAsync(cancellationToken);
        db.BillRelatedItems.RemoveRange(billRelated);

        // 5. Bills
        var bills = await db.Bills
            .Where(b => b.CreatedBy == userId)
            .ToListAsync(cancellationToken);
        db.Bills.RemoveRange(bills);

        // 6. Budget transfers, occurrences, budgets
        var userBudgetIds = await db.Budgets
            .Where(b => b.CreatedBy == userId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var budgetOccIds = await db.BudgetOccurrences
            .Where(o => userBudgetIds.Contains(o.BudgetId))
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var transfers = await db.BudgetTransfers
            .Where(t => budgetOccIds.Contains(t.SourceOccurrenceId) ||
                         budgetOccIds.Contains(t.DestinationOccurrenceId))
            .ToListAsync(cancellationToken);
        db.BudgetTransfers.RemoveRange(transfers);

        var budgetOccs = await db.BudgetOccurrences
            .Where(o => userBudgetIds.Contains(o.BudgetId))
            .ToListAsync(cancellationToken);
        db.BudgetOccurrences.RemoveRange(budgetOccs);

        var budgets = await db.Budgets
            .Where(b => b.CreatedBy == userId)
            .ToListAsync(cancellationToken);
        db.Budgets.RemoveRange(budgets);

        // 7. Shopping items and lists
        var userListIds = await db.ShoppingLists
            .Where(l => l.CreatedBy == userId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var shoppingItems = await db.ShoppingItems
            .Where(i => userListIds.Contains(i.ShoppingListId))
            .ToListAsync(cancellationToken);
        db.ShoppingItems.RemoveRange(shoppingItems);

        var shoppingLists = await db.ShoppingLists
            .Where(l => l.CreatedBy == userId)
            .ToListAsync(cancellationToken);
        db.ShoppingLists.RemoveRange(shoppingLists);

        // 8. Task occurrences, recurrence, tasks
        var userTaskIds = await db.HouseholdTasks
            .Where(t => t.CreatedBy == userId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var taskOccs = await db.TaskOccurrences
            .Where(o => userTaskIds.Contains(o.HouseholdTaskId))
            .ToListAsync(cancellationToken);
        db.TaskOccurrences.RemoveRange(taskOccs);

        var recurrencePatterns = await db.RecurrencePatterns
            .Include(r => r.Assignees)
            .Where(r => userTaskIds.Contains(r.HouseholdTaskId))
            .ToListAsync(cancellationToken);
        foreach (var pattern in recurrencePatterns)
        {
            db.RecurrenceAssignees.RemoveRange(pattern.Assignees);
        }
        db.RecurrencePatterns.RemoveRange(recurrencePatterns);

        var tasks = await db.HouseholdTasks
            .Where(t => t.CreatedBy == userId)
            .ToListAsync(cancellationToken);
        db.HouseholdTasks.RemoveRange(tasks);

        // 9. User connections
        var connections = await db.UserConnections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId)
            .ToListAsync(cancellationToken);
        db.UserConnections.RemoveRange(connections);

        // 10. Audit logs
        var auditLogs = await db.AuditLogs
            .Include(a => a.HistoryEntries)
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var log in auditLogs)
        {
            db.AuditHistoryEntries.RemoveRange(log.HistoryEntries);
        }
        db.AuditLogs.RemoveRange(auditLogs);

        await db.SaveChangesAsync(cancellationToken);

        // 11. Delete the user account
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            // Delete refresh tokens
            var refreshTokens = await db.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync(cancellationToken);
            db.RefreshTokens.RemoveRange(refreshTokens);
            await db.SaveChangesAsync(cancellationToken);

            await userManager.DeleteAsync(user);
        }

        // 12. Clean up fake friend accounts that have no other connections
        foreach (var (_, _, email) in FakeFriends)
        {
            var friend = await userManager.FindByEmailAsync(email);
            if (friend is null) continue;

            var hasOtherConnections = await db.UserConnections
                .AnyAsync(c => (c.RequesterId == friend.Id || c.AddresseeId == friend.Id) &&
                               c.Status == ConnectionStatus.Accepted, cancellationToken);

            if (!hasOtherConnections)
            {
                // Also clean up any refresh tokens
                var friendTokens = await db.RefreshTokens
                    .Where(t => t.UserId == friend.Id)
                    .ToListAsync(cancellationToken);
                db.RefreshTokens.RemoveRange(friendTokens);
                await db.SaveChangesAsync(cancellationToken);

                await userManager.DeleteAsync(friend);
                logger.LogInformation("Deleted orphaned demo friend: {Email}", email);
            }
        }

        logger.LogInformation("Demo data purge complete for user {UserId}", userId);
    }
}
