using BlazorUI.Components.Bills;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Bills;

public partial class BillDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    BillDetailDto? Bill { get; set; }

    bool IsLoading { get; set; }

    bool IsProcessing { get; set; }

    ApiProblemDetails? Error { get; set; }

    CancellationTokenSource _cts = new();

    protected override async Task OnParametersSetAsync()
    {
        await LoadBillAsync();
    }

    async Task LoadBillAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BillService.GetBillByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Bill = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task EditBillAsync()
    {
        if (Bill is null) return;

        var model = BillFormModel.FromDetail(Bill);

        var result = await DialogService.OpenAsync<BillFormDialog>(
            "Edit Bill",
            new Dictionary<string, object>
            {
                { nameof(BillFormDialog.Model), model },
                { nameof(BillFormDialog.IsEdit), true }
            },
            new DialogOptions
            {
                Width = "700px",
                Height = "600px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        if (result is BillFormModel formModel)
        {
            var request = formModel.ToUpdateRequest();
            var updateResult = await BillService.UpdateBillAsync(Bill.Id, request, _cts.Token);

            if (updateResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Bill Updated",
                    Detail = $"'{formModel.Title}' has been updated successfully.",
                    Duration = 4000
                });
                await LoadBillAsync();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = updateResult.Problem.Detail,
                    Duration = 6000
                });
            }
        }
    }

    async Task DeleteBillAsync()
    {
        if (Bill is null) return;

        var confirmed = await DialogService.OpenAsync<BillDeleteConfirm>(
            "Delete Bill",
            new Dictionary<string, object>
            {
                { nameof(BillDeleteConfirm.BillTitle), Bill.Title },
                { nameof(BillDeleteConfirm.Amount), Bill.Amount },
                { nameof(BillDeleteConfirm.Currency), Bill.Currency }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var deleteResult = await BillService.DeleteBillAsync(Bill.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Bill Deleted",
                    Detail = $"'{Bill.Title}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/bills");
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.Detail,
                    Duration = 6000
                });
            }
        }
    }

    async Task MarkSplitAsPaidAsync(BillSplitDto split)
    {
        if (Bill is null) return;

        IsProcessing = true;
        var result = await BillService.MarkSplitAsPaidAsync(Bill.Id, split.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Split Marked as Paid",
                Detail = "The split has been marked as paid.",
                Duration = 4000
            });
            await LoadBillAsync();
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.Detail,
                Duration = 6000
            });
        }

        IsProcessing = false;
    }

    void GoBack()
    {
        NavigationManager.NavigateTo("/bills");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
