using BlazorUI.Components.Admin;
using BlazorUI.Components.Common;
using BlazorUI.Models.Common;
using BlazorUI.Models.Exceptions;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Admin.Exceptions;

public partial class ExceptionDashboard : IDisposable
{
    [Inject]
    IExceptionLogService ExceptionLogService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    PaginatedList<ExceptionLogBriefDto> ExceptionData { get; set; } = new();
    ExceptionSummaryDto? Summary { get; set; }

    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }

    // Filters
    string? SearchTerm { get; set; }
    int? SeverityFilter { get; set; }
    bool? HandledFilter { get; set; }
    string? ExceptionTypeFilter { get; set; }
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }

    int _currentPage = 1;
    const int PageSize = 20;

    readonly CancellationTokenSource _cts = new();

    static readonly object[] SeverityOptions =
    [
        new { Text = "Low", Value = (int?)0 },
        new { Text = "Medium", Value = (int?)1 },
        new { Text = "High", Value = (int?)2 },
        new { Text = "Critical", Value = (int?)3 }
    ];

    static readonly object[] HandledOptions =
    [
        new { Text = "Handled", Value = (bool?)true },
        new { Text = "Unhandled", Value = (bool?)false }
    ];

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadExceptionsAsync(), LoadSummaryAsync());
    }

    async Task LoadExceptionsAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await ExceptionLogService.GetExceptionsAsync(
            pageNumber: _currentPage,
            pageSize: PageSize,
            severity: SeverityFilter,
            isHandled: HandledFilter,
            exceptionType: ExceptionTypeFilter,
            searchTerm: SearchTerm,
            from: FromDate,
            to: ToDate,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            ExceptionData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task LoadSummaryAsync()
    {
        var result = await ExceptionLogService.GetSummaryAsync(
            from: FromDate, to: ToDate, cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            Summary = result.Value;
        }
    }

    async Task OnLoadDataAsync(LoadDataArgs args)
    {
        _currentPage = (args.Skip ?? 0) / PageSize + 1;
        await LoadExceptionsAsync();
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await Task.WhenAll(LoadExceptionsAsync(), LoadSummaryAsync());
    }

    async Task OnClearFiltersAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        SeverityFilter = null;
        HandledFilter = null;
        ExceptionTypeFilter = null;
        FromDate = null;
        ToDate = null;
        await Task.WhenAll(LoadExceptionsAsync(), LoadSummaryAsync());
    }

    async Task ViewException(ExceptionLogBriefDto item)
    {
        var result = await ExceptionLogService.GetExceptionByIdAsync(item.Id, _cts.Token);

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

    async Task DeleteException(ExceptionLogBriefDto item)
    {
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Exception",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), "Are you sure you want to delete this exception log entry?" },
                { nameof(ConfirmDialog.ConfirmText), "Delete" },
                { nameof(ConfirmDialog.ConfirmIcon), "delete" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger }
            },
            new DialogOptions
            {
                Width = "420px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var result = await ExceptionLogService.DeleteExceptionAsync(item.Id, _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Deleted",
                    Detail = "Exception log entry deleted.",
                    Duration = 4000
                });
                await Task.WhenAll(LoadExceptionsAsync(), LoadSummaryAsync());
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
    }

    static string GetSeverityIcon(int severity) => severity switch
    {
        0 => "info",
        1 => "warning",
        2 => "error",
        3 => "dangerous",
        _ => "help"
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
