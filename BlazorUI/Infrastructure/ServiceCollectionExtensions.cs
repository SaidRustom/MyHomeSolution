using BlazorUI.Infrastructure.Auth;
using BlazorUI.Infrastructure.Configuration;
using BlazorUI.Infrastructure.Realtime;
using BlazorUI.Services;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Realtime;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorUI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var apiSettings = configuration
            .GetSection(ApiSettings.SectionName)
            .Get<ApiSettings>() ?? new ApiSettings();

        services.AddAuthorizationCore();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());

        // Auth service uses an un-authenticated HttpClient (login/register/refresh don't need tokens)
        services.AddHttpClient<IAuthService, AuthService>(client =>
            ConfigureClient(client, apiSettings));

        // Portfolio service is public (no auth required)
        services.AddHttpClient<IPortfolioService, PortfolioService>(client =>
            ConfigureClient(client, apiSettings));

        return services;
    }

    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var apiSettings = configuration
            .GetSection(ApiSettings.SectionName)
            .Get<ApiSettings>() ?? new ApiSettings();

        services.AddTransient<AuthTokenHandler>();

        services.AddHttpClient<ITaskService, TaskService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IOccurrenceService, OccurrenceService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IBillService, BillService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IUserService, UserService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<INotificationService, NotificationService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IShareService, ShareService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IShoppingListService, ShoppingListService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IUserConnectionService, UserConnectionService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IExceptionLogService, ExceptionLogService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IBackgroundServiceMonitorService, BackgroundServiceMonitorService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IDashboardService, DashboardService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IAvatarService, AvatarService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IAuditService, AuditService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IBudgetService, BudgetService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IDemoService, DemoService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddHttpClient<IPortfolioAdminService, PortfolioAdminService>(client =>
            ConfigureClient(client, apiSettings))
            .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }

    public static IServiceCollection AddRealtimeServices(this IServiceCollection services)
    {
        services.AddScoped<HubConnectionManager>();
        services.AddScoped<ITaskHubClient, TaskHubClient>();
        services.AddScoped<INotificationHubClient, NotificationHubClient>();

        return services;
    }

    private static void ConfigureClient(HttpClient client, ApiSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
        }

        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }
}
