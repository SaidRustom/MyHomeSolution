using BlazorUI.Components.Common;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;

namespace BlazorUI.Components.Bills;

public partial class ReceiptScanDialog
{
    [Inject]
    private IBillService BillService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private BillCategory SelectedCategory { get; set; } = BillCategory.Groceries;
    private List<BillSplitFormModel> Splits { get; set; } = [];
    private string? CurrentUserId { get; set; }
    private ImageCaptureResult? CapturedImage { get; set; }
    private string? PreviewUrl { get; set; }
    private bool IsProcessing { get; set; }
    private string? ErrorMessage { get; set; }

    private IEnumerable<BillCategory> Categories => Enum.GetValues<BillCategory>();

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        CurrentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;
    }

    private void OnImageCaptured(ImageCaptureResult result)
    {
        CapturedImage = result;
        PreviewUrl = $"data:{result.ContentType};base64,{Convert.ToBase64String(result.Data)}";
        ErrorMessage = null;
    }

    private async Task SubmitAsync()
    {
        if (CapturedImage is null)
        {
            ErrorMessage = "Please capture or upload a receipt image first.";
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            using var stream = new MemoryStream(CapturedImage.Data);
            var splits = Splits
                .Where(s => !string.IsNullOrEmpty(s.UserId))
                .Select(s => new SplitRequest(s.UserId, s.Percentage))
                .ToList();

            var result = await BillService.CreateBillFromReceiptAsync(
                stream,
                CapturedImage.FileName,
                CapturedImage.ContentType,
                SelectedCategory,
                splits.Count > 0 ? splits : null);

            if (result.IsSuccess)
            {
                DialogService.Close(result.Value);
            }
            else
            {
                ErrorMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to process receipt. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    private void Cancel() => DialogService.Close(null);
}
