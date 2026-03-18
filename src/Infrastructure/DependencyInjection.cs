using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Configuration;
using MyHomeSolution.Infrastructure.Identity;
using MyHomeSolution.Infrastructure.Persistence;
using MyHomeSolution.Infrastructure.Services;

namespace MyHomeSolution.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices(configuration);
        services.AddBackgroundServices(configuration);
        services.AddRealtime();
        services.AddReceiptAnalysis(configuration);
        services.AddEmailServices(configuration);

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ITaskProcessingLock, TaskProcessingLock>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IExceptionLogService, ExceptionLogService>();
        services.AddSingleton<IFileStorageService>(sp =>
        {
            var env = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var basePath = Path.Combine(env.ContentRootPath, "uploads");
            return new LocalFileStorageService(basePath);
        });

        services.AddExceptionAnalysis(configuration);

        return services;
    }

    private static void AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotificationService, SignalRNotificationService>();
    }

    private static void AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, builder =>
                builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
    }

    private static void AddIdentityServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = "name"
                };
            });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
    }

    private static void AddBackgroundServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OccurrenceGeneratorOptions>()
            .Bind(configuration.GetSection(OccurrenceGeneratorOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<OverdueOccurrenceOptions>()
            .Bind(configuration.GetSection(OverdueOccurrenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<BudgetOccurrenceGeneratorOptions>()
            .Bind(configuration.GetSection(BudgetOccurrenceGeneratorOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IOccurrenceScheduler, OccurrenceScheduler>();
        services.AddHostedService<OccurrenceGeneratorService>();
        services.AddHostedService<OverdueOccurrenceService>();
        services.AddHostedService<BudgetOccurrenceGeneratorService>();
    }

    private static void AddReceiptAnalysis(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IReceiptAnalysisService, OpenAiReceiptAnalysisService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IShoppingItemGroupingService, OpenAiShoppingItemGroupingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }

    private static void AddEmailServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MailgunOptions>()
            .Bind(configuration.GetSection(MailgunOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmailBackgroundQueue, EmailBackgroundQueue>();

        services.AddHttpClient<IEmailService, MailgunEmailService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHostedService<EmailBackgroundService>();
    }

    private static void AddExceptionAnalysis(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IExceptionAnalysisService, OpenAiExceptionAnalysisService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }
}
