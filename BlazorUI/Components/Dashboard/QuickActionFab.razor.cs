using BlazorUI.Components.Bills;
using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Dashboard;

public partial class QuickActionFab
{
    [Inject] DialogService DialogService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    bool _isOpen;

    void Toggle() => _isOpen = !_isOpen;
    void Close() => _isOpen = false;

    async Task CreateBillManual()
    {
        Close();
        var result = await DialogService.OpenAsync<BillFormDialog>(
            "New Bill",
            new Dictionary<string, object>
            {
                ["Model"] = new BillFormModel(),
                ["IsEdit"] = false
            },
            new DialogOptions
            {
                Width = "min(95vw, 600px)",
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });

        if (result is Guid billId)
            NavigationManager.NavigateTo($"/bills/{billId}");
    }

    async Task CreateBillFromReceipt()
    {
        Close();
        // Navigate to bills page with receipt mode — the bill form dialog supports receipt upload
        var result = await DialogService.OpenAsync<BillFormDialog>(
            "New Bill from Receipt",
            new Dictionary<string, object>
            {
                ["Model"] = new BillFormModel(),
                ["IsEdit"] = false
            },
            new DialogOptions
            {
                Width = "min(95vw, 600px)",
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });

        if (result is Guid billId)
            NavigationManager.NavigateTo($"/bills/{billId}");
    }
}
