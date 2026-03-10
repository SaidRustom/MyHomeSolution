using BlazorUI.Components.Admin;
using BlazorUI.Models.BackgroundServices;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Admin.BackgroundServices;

public partial class BackgroundServiceDashboard : IDisposable
{
    [Inject]
    IBackgroundServiceMonitorService MonitorService { get; set; } = default!;

    [Inject]
    IExceptionLogService ExceptionLogService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    IReadOnlyList<BackgroundServiceDto> Services { get; set; } = [];
    BackgroundServiceDto? SelectedService { get; set; }
    PaginatedList<BackgroundServiceLogBriefDto> LogData { get; set; } = new();

    bool IsLoading { get; set; }
    bool IsLoadingLogs { get; set; }
    ApiProblemDetails? Error { get; set; }

    int _logPage = 1;
    const int LogPageSize = 15;

    readonly CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadServicesAsync();
    }

    async Task LoadServicesAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await MonitorService.GetServicesAsync(_cts.Token);

        if (result.IsSuccess)
        {
            Services = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task SelectService(BackgroundServiceDto service)
    {
        SelectedService = service;
        _logPage = 1;
        await LoadLogsAsync();
    }

    async Task LoadLogsAsync()
    {
        if (SelectedService is null) return;

        IsLoadingLogs = true;

        var result = await MonitorService.GetLogsAsync(
            SelectedService.Id, _logPage, LogPageSize, _cts.Token);

        if (result.IsSuccess)
        {
            LogData = result.Value;
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 6000
            });
        }

        IsLoadingLogs = false;
    }

    async Task OnLogLoadDataAsync(LoadDataArgs args)
    {
        _logPage = (args.Skip ?? 0) / LogPageSize + 1;
        await LoadLogsAsync();
    }

    async Task ViewExceptionAsync(Guid exceptionLogId)
    {
        var result = await ExceptionLogService.GetExceptionByIdAsync(exceptionLogId, _cts.Token);

        if (result.IsSuccess)
        {
            await DialogService.OpenAsync<ExceptionDetailDialog>(
                "Exception Details",
                new Dictionary<string, object>
                {
                    { nameof(ExceptionDetailDialog.Exception), result.Value }
                },
                new DialogOptions
                {
                    Width = "800px",
                    Height = "90vh",
                    CloseDialogOnOverlayClick = true,
                    ShowClose = true
                });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 6000
            });
        }
    }

    static string GetServiceStatusColor(BackgroundServiceDto service)
    {
        if (!service.IsEnabled) return "var(--rz-secondary)";
        return service.LatestLog?.Status switch
        {
            "Completed" => "var(--rz-success)",
            "Failed" => "var(--rz-danger)",
            "Running" => "var(--rz-info)",
            _ => "var(--rz-primary)"
        };
    }

    static string GetServiceIcon(BackgroundServiceDto service)
    {
        if (!service.IsEnabled) return "pause_circle";
        return service.LatestLog?.Status switch
        {
            "Completed" => "check_circle",
            "Failed" => "error",
            "Running" => "sync",
            _ => "home_repair_service"
        };
    }

    static BadgeStyle GetLogBadgeStyle(string status) => status switch
    {
        "Completed" => BadgeStyle.Success,
        "Failed" => BadgeStyle.Danger,
        "Running" => BadgeStyle.Info,
        _ => BadgeStyle.Light
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
