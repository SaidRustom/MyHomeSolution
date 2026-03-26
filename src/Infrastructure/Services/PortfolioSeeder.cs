using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Infrastructure.Services;

public static class PortfolioSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PortfolioProfile>>();

        var hasProfile = await dbContext.PortfolioProfiles.AnyAsync();
        if (hasProfile) return;

        logger.LogInformation("Seeding portfolio data...");

        // ── Profile ──────────────────────────────────────────────────────
        var profile = new PortfolioProfile
        {
            FullName = "Said Rustom",
            Headline = "Full-Stack Software Engineer & Cloud Architect",
            SubHeadline = "Building scalable, elegant solutions with .NET, Azure, and modern web technologies.",
            Bio = "I'm a passionate software engineer with a deep love for building products that make a real difference. I specialize in full-stack development using the .NET ecosystem, Blazor, and cloud-native architectures on Azure.\n\nFrom designing robust APIs to crafting pixel-perfect user interfaces, I thrive at the intersection of engineering excellence and user experience. I believe great software is built with empathy, attention to detail, and a relentless drive for quality.",
            Email = "contact@saidrustom.ca",
            Location = "Canada",
            GitHubUrl = "https://github.com/SaidRustom",
            LinkedInUrl = "https://linkedin.com/in/saidrustom",
            IsActive = true
        };

        dbContext.PortfolioProfiles.Add(profile);

        // ── Experiences ──────────────────────────────────────────────────
        dbContext.PortfolioExperiences.AddRange(
            new PortfolioExperience
            {
                Company = "MyHomeSolution",
                Role = "Founder & Lead Developer",
                Description = "Designed and built a comprehensive household management platform from the ground up. Full-stack development using .NET 10, Blazor WebAssembly, Entity Framework Core, and SQL Server. Implemented real-time notifications via SignalR, budgeting with automated occurrence generation, bill tracking & splitting, task scheduling with recurrence patterns, and a modular CQRS architecture with MediatR.",
                Technologies = ".NET 10, Blazor WASM, EF Core, SQL Server, SignalR, MediatR, Azure",
                StartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsCurrent = true,
                SortOrder = 0,
                IsVisible = true
            },
            new PortfolioExperience
            {
                Company = "Freelance",
                Role = "Software Consultant",
                Description = "Delivered custom software solutions for small businesses and startups. Focused on web applications, API integrations, and cloud migrations. Helped clients modernize legacy systems and adopt cloud-native architectures.",
                Technologies = "C#, ASP.NET Core, React, Azure, Docker, PostgreSQL",
                StartDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsCurrent = false,
                SortOrder = 1,
                IsVisible = true
            }
        );

        // ── Projects ─────────────────────────────────────────────────────
        dbContext.PortfolioProjects.AddRange(
            new PortfolioProject
            {
                Title = "MyHomeSolution",
                ShortDescription = "A full-featured household management platform with budgeting, bill tracking, task scheduling, shopping lists, and real-time collaboration.",
                LongDescription = "A comprehensive household management ecosystem built with .NET 10 and Blazor WebAssembly. Features include automated budget occurrence generation, intelligent bill splitting with receipt OCR, a drag-and-drop task scheduler with recurrence patterns, real-time push notifications via SignalR, and a robust CQRS architecture. The platform supports multi-user households with granular sharing permissions and a beautiful, responsive Radzen-based UI.",
                GitHubUrl = "https://github.com/SaidRustom/MyHomeSolution",
                Technologies = ".NET 10, Blazor WASM, EF Core, SQL Server, SignalR, MediatR, Radzen, OpenAI",
                Category = "Full-Stack",
                SortOrder = 0,
                IsFeatured = true,
                IsVisible = true
            },
            new PortfolioProject
            {
                Title = "Portfolio Platform",
                ShortDescription = "This very website — a database-driven personal portfolio built with Blazor WebAssembly featuring smooth animations, dark theme, and dynamic content management.",
                Technologies = "Blazor WASM, .NET 10, CSS3, EF Core",
                Category = "Frontend",
                SortOrder = 1,
                IsFeatured = false,
                IsVisible = true
            }
        );

        // ── Skills ───────────────────────────────────────────────────────
        dbContext.PortfolioSkills.AddRange(
            // Backend
            new PortfolioSkill { Name = "C# / .NET", Category = "Backend", ProficiencyLevel = 95, IconClass = "⚙️", SortOrder = 0, IsVisible = true },
            new PortfolioSkill { Name = "ASP.NET Core", Category = "Backend", ProficiencyLevel = 95, IconClass = "🌐", SortOrder = 1, IsVisible = true },
            new PortfolioSkill { Name = "Entity Framework Core", Category = "Backend", ProficiencyLevel = 90, IconClass = "🗄️", SortOrder = 2, IsVisible = true },
            new PortfolioSkill { Name = "MediatR / CQRS", Category = "Backend", ProficiencyLevel = 90, IconClass = "📨", SortOrder = 3, IsVisible = true },
            new PortfolioSkill { Name = "SignalR", Category = "Backend", ProficiencyLevel = 85, IconClass = "📡", SortOrder = 4, IsVisible = true },
            new PortfolioSkill { Name = "REST API Design", Category = "Backend", ProficiencyLevel = 92, IconClass = "🔗", SortOrder = 5, IsVisible = true },

            // Frontend
            new PortfolioSkill { Name = "Blazor (WASM & Server)", Category = "Frontend", ProficiencyLevel = 95, IconClass = "🔥", SortOrder = 0, IsVisible = true },
            new PortfolioSkill { Name = "HTML / CSS / JS", Category = "Frontend", ProficiencyLevel = 88, IconClass = "🎨", SortOrder = 1, IsVisible = true },
            new PortfolioSkill { Name = "Radzen Components", Category = "Frontend", ProficiencyLevel = 90, IconClass = "🧩", SortOrder = 2, IsVisible = true },
            new PortfolioSkill { Name = "Responsive Design", Category = "Frontend", ProficiencyLevel = 85, IconClass = "📱", SortOrder = 3, IsVisible = true },

            // Cloud & DevOps
            new PortfolioSkill { Name = "Microsoft Azure", Category = "Cloud & DevOps", ProficiencyLevel = 85, IconClass = "☁️", SortOrder = 0, IsVisible = true },
            new PortfolioSkill { Name = "SQL Server", Category = "Cloud & DevOps", ProficiencyLevel = 88, IconClass = "🛢️", SortOrder = 1, IsVisible = true },
            new PortfolioSkill { Name = "Docker", Category = "Cloud & DevOps", ProficiencyLevel = 75, IconClass = "🐳", SortOrder = 2, IsVisible = true },
            new PortfolioSkill { Name = "CI/CD Pipelines", Category = "Cloud & DevOps", ProficiencyLevel = 80, IconClass = "🔄", SortOrder = 3, IsVisible = true },
            new PortfolioSkill { Name = "Git", Category = "Cloud & DevOps", ProficiencyLevel = 90, IconClass = "📝", SortOrder = 4, IsVisible = true },

            // Architecture
            new PortfolioSkill { Name = "Clean Architecture", Category = "Architecture & Patterns", ProficiencyLevel = 90, IconClass = "🏛️", SortOrder = 0, IsVisible = true },
            new PortfolioSkill { Name = "Domain-Driven Design", Category = "Architecture & Patterns", ProficiencyLevel = 85, IconClass = "🧠", SortOrder = 1, IsVisible = true },
            new PortfolioSkill { Name = "Unit & Integration Testing", Category = "Architecture & Patterns", ProficiencyLevel = 82, IconClass = "🧪", SortOrder = 2, IsVisible = true },
            new PortfolioSkill { Name = "SOLID Principles", Category = "Architecture & Patterns", ProficiencyLevel = 92, IconClass = "💎", SortOrder = 3, IsVisible = true }
        );

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Portfolio data seeded successfully.");
    }
}
