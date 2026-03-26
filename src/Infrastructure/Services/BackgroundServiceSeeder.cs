using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Seeds <see cref="BackgroundServiceDefinition"/> rows for every
/// <see cref="IMonitoredBackgroundService"/> registered in DI.
/// Called once at application startup.
/// </summary>
public static class BackgroundServiceSeeder
{
    /// <summary>
    /// Well-known service IDs — deterministic so they survive re-deployments.
    /// </summary>
    public static class ServiceIds
    {
        public static readonly Guid OccurrenceGenerator =
            Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001");

        public static readonly Guid OverdueOccurrence =
            Guid.Parse("a1b2c3d4-0002-0002-0002-000000000002");

        public static readonly Guid EmailBackground =
            Guid.Parse("a1b2c3d4-0003-0003-0003-000000000003");

        public static readonly Guid BudgetOccurrenceGenerator =
            Guid.Parse("a1b2c3d4-0004-0004-0004-000000000004");

        public static readonly Guid DemoUserCleanup =
            Guid.Parse("a1b2c3d4-0005-0005-0005-000000000005");
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackgroundServiceDefinition>>();

        var definitions = new (Guid Id, string Name, string Description, string TypeName)[]
        {
            (ServiceIds.OccurrenceGenerator,
             "Occurrence Generator",
             "Generates future task occurrences for recurring household tasks based on their recurrence patterns.",
             typeof(OccurrenceGeneratorService).FullName!),

            (ServiceIds.OverdueOccurrence,
             "Overdue Occurrence Checker",
             "Scans for pending task occurrences that have passed their due date and marks them as overdue.",
             typeof(OverdueOccurrenceService).FullName!),

            (ServiceIds.EmailBackground,
             "Email Sender",
             "Processes the background email queue and sends emails via the configured email provider.",
             typeof(EmailBackgroundService).FullName!),

            (ServiceIds.BudgetOccurrenceGenerator,
             "Budget Occurrence Generator",
             "Creates new budget occurrences when recurring budget periods expire and handles fund carryover.",
             typeof(BudgetOccurrenceGeneratorService).FullName!),

            (ServiceIds.DemoUserCleanup,
             "Demo User Cleanup",
             "Periodically checks for expired demo user sessions and purges all their data after 24 hours.",
             typeof(DemoUserCleanupService).FullName!)
        };

        foreach (var (id, name, description, typeName) in definitions)
        {
            var exists = await dbContext.BackgroundServiceDefinitions
                .AsNoTracking()
                .AnyAsync(d => d.Id == id);

            if (exists) continue;

            var entity = new BackgroundServiceDefinition
            {
                Name = name,
                Description = description,
                QualifiedTypeName = typeName,
                IsEnabled = true,
                RegisteredAt = dateTimeProvider.UtcNow
            };

            // BaseEntity.Id has a protected setter — use the EF entry to set it
            dbContext.BackgroundServiceDefinitions.Add(entity);
            dbContext.Entry(entity).Property(e => e.Id).CurrentValue = id;

            logger.LogInformation("Seeding background service definition: {Name}", name);
        }

        await dbContext.SaveChangesAsync();
    }
}
