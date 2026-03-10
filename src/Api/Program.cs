using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Api.Middleware;
using MyHomeSolution.Api.Services;
using MyHomeSolution.Application;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure;
using MyHomeSolution.Infrastructure.Hubs;
using MyHomeSolution.Infrastructure.Identity;
using MyHomeSolution.Infrastructure.Persistence;
using MyHomeSolution.Infrastructure.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MyHomeSolution API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    // ── Application & Infrastructure ────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── HTTP / Auth ─────────────────────────────────────────────────────────
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers();
    builder.Services.AddAuthorization();

    // ── CORS ────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:Origins")
                .Get<string[]>() ?? ["https://localhost:7096"];

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ── OpenAPI ──────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Health checks ───────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(name: "database");

    // ── Response compression ────────────────────────────────────────────────
    builder.Services.AddResponseCompression(options =>
        options.EnableForHttps = true);

    // ── Rate limiting ───────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", limiter =>
        {
            limiter.PermitLimit = 60;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 5;
        });
    });

    // ── Build ───────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Database (dev-only auto-migrate & seed) ───────────────────────────
    if (app.Environment.IsDevelopment())
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await IdentityDataSeeder.SeedRolesAsync(scope.ServiceProvider);
        await BackgroundServiceSeeder.SeedAsync(scope.ServiceProvider);

        var seedConfig = app.Configuration.GetSection("SeedAdmin");
        var seedEmail = seedConfig["Email"];
        if (!string.IsNullOrEmpty(seedEmail))
        {
            await IdentityDataSeeder.SeedDefaultAdminAsync(
                scope.ServiceProvider,
                seedEmail,
                seedConfig["Password"] ?? "Admin@123456",
                seedConfig["FirstName"] ?? "System",
                seedConfig["LastName"] ?? "Administrator");
        }
    }

    // ── Middleware pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseResponseCompression();
    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoints ───────────────────────────────────────────────────────────
    app.MapControllers().RequireRateLimiting("fixed");
    app.MapHub<TaskHub>("/hubs/tasks");
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapOpenApi();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
