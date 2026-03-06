using BlazorUI.Components.Bills;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;


namespace BlazorUI.Pages.Bills;

public partial class BillList : IDisposable
{
    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    static readonly DialogOptions BillDialogOptions = new()
    {
        Width = "700px",
        Height = "600px",
        Resizable = true,
        Draggable = true,
        CloseDialogOnOverlayClick = false,
        ShowClose = false
    };

    PaginatedList<BillBriefDto> BillData { get; set; } = new();

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    // Filter state
    string? SearchTerm { get; set; }
    BillCategory? SelectedCategory { get; set; }
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }

    int _currentPage = 1;
    const int PageSize = 20;

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadBillsAsync();
    }

    async Task LoadBillsAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BillService.GetBillsAsync(
            pageNumber: _currentPage,
            pageSize: PageSize,
            category: SelectedCategory,
            searchTerm: SearchTerm,
            fromDate: FromDate,
            toDate: ToDate,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            BillData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task OnLoadDataAsync(LoadDataArgs args)
    {
        _currentPage = (args.Skip ?? 0) / PageSize + 1;
        await LoadBillsAsync();
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await LoadBillsAsync();
    }

    async Task OnClearFiltersAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        SelectedCategory = null;
        FromDate = null;
        ToDate = null;
        await LoadBillsAsync();
    }

    void ViewBill(BillBriefDto bill)
    {
        NavigationManager.NavigateTo($"/bills/{bill.Id}");
    }

    async Task CreateBillAsync()
    {
        var model = new BillFormModel();

        var result = await DialogService.OpenAsync<BillFormDialog>(
            "Create Bill",
            new Dictionary<string, object>
            {
                { nameof(BillFormDialog.Model), model },
                { nameof(BillFormDialog.IsEdit), false }
            },
            BillDialogOptions);

        if (result is true)
        {
            await LoadBillsAsync();
        }
    }

    async Task ScanReceiptAsync()
    {
        var result = await DialogService.OpenAsync<ReceiptScanDialog>(
            "Scan Receipt",
            parameters: null,
            new DialogOptions
            {
                Width = "600px",
                Height = "700px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        if (result is Models.Bills.BillDetailDto billDetail)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Bill Created from Receipt",
                Detail = $"'{billDetail.Title}' — {billDetail.Currency}{billDetail.Amount:N2} with {billDetail.Items.Count} items.",
                Duration = 5000
            });

            NavigationManager.NavigateTo($"/bills/{billDetail.Id}");
        }
    }

    async Task DeleteBillAsync(BillBriefDto bill)
    {
        var confirmed = await DialogService.OpenAsync<BillDeleteConfirm>(
            "Delete Bill",
            new Dictionary<string, object>
            {
                { nameof(BillDeleteConfirm.BillTitle), bill.Title },
                { nameof(BillDeleteConfirm.Amount), bill.Amount },
                { nameof(BillDeleteConfirm.Currency), bill.Currency }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var deleteResult = await BillService.DeleteBillAsync(bill.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Bill Deleted",
                    Detail = $"'{bill.Title}' has been deleted.",
                    Duration = 4000
                });
                await LoadBillsAsync();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
