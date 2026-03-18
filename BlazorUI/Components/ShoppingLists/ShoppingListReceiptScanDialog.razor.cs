using BlazorUI.Components.Bills;
using BlazorUI.Components.Common;
using BlazorUI.Models.Common;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;

namespace BlazorUI.Components.ShoppingLists;

public partial class ShoppingListReceiptScanDialog
{
    [Inject]
    private IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private NotificationService Notifications { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid ShoppingListId { get; set; }

    private List<BillSplitFormModel> _splits = [];
    private string? _currentUserId;
    private ImageCaptureResult? _capturedImage;
    private bool _isProcessing;
    private bool _isResolvingMatch;
    private string? _errorMessage;
    private ProcessReceiptResultDto? _result;
    private readonly HashSet<string> _resolvedMatches = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        await base.OnInitializedAsync();
    }

    private void OnImageCaptured(ImageCaptureResult result)
    {
        _capturedImage = result;
        _errorMessage = null;
    }

    private async Task ProcessAsync()
    {
        if (_capturedImage is null)
        {
            _errorMessage = "Please capture or upload a receipt image first.";
            return;
        }

        _isProcessing = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            using var stream = new MemoryStream(_capturedImage.Data);
            var splits = _splits
                .Where(s => !string.IsNullOrEmpty(s.UserId))
                .Select(s => new SplitRequest(s.UserId, s.Percentage))
                .ToList();

            var result = await ShoppingListService.ProcessReceiptAsync(
                ShoppingListId, stream, _capturedImage.FileName, _capturedImage.ContentType,
                splits.Count > 0 ? splits : null);

            if (result.IsSuccess)
            {
                var bill = result.Value.Bill;
                var itemsSum = bill.Items.Sum(i => i.Price);

                _result = result.Value;
            }
            else
            {
                _errorMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to process receipt. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ResolveMatchAsync(CrossListMatchDto match, CrossListTargetDto target, bool toggleExisting)
    {
        if (_result is null) return;

        _isResolvingMatch = true;
        StateHasChanged();

        try
        {
            var request = new ResolveCrossListMatchRequest
            {
                TargetShoppingListId = target.ShoppingListId,
                BillId = _result.BillId,
                ReceiptItemName = match.ReceiptItemName,
                GenericName = match.GenericName,
                Price = match.Price,
                IsTaxable = match.IsTaxable,
                ToggleExisting = toggleExisting,
                ShoppingItemId = toggleExisting ? target.ShoppingItemId : null
            };

            var result = await ShoppingListService.ResolveCrossListMatchAsync(
                target.ShoppingListId, request);

            if (result.IsSuccess)
            {
                _resolvedMatches.Add(match.ReceiptItemName);
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Item Resolved",
                    Detail = toggleExisting
                        ? $"'{match.GenericName}' checked off in '{target.ShoppingListTitle}'"
                        : $"'{match.GenericName}' added to '{target.ShoppingListTitle}'",
                    Duration = 3000
                });
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Failed to resolve match. Please try again.",
                    Duration = 5000
                });
            }
        }
        finally
        {
            _isResolvingMatch = false;
            StateHasChanged();
        }
    }

    private async Task AddToCurrentListAsync(CrossListMatchDto match)
    {
        if (_result is null) return;

        _isResolvingMatch = true;
        StateHasChanged();

        try
        {
            var request = new ResolveCrossListMatchRequest
            {
                TargetShoppingListId = ShoppingListId,
                BillId = _result.BillId,
                ReceiptItemName = match.ReceiptItemName,
                GenericName = match.GenericName,
                Price = match.Price,
                IsTaxable = match.IsTaxable,
                ToggleExisting = false
            };

            var result = await ShoppingListService.ResolveCrossListMatchAsync(
                ShoppingListId, request);

            if (result.IsSuccess)
            {
                _resolvedMatches.Add(match.ReceiptItemName);
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Item Added",
                    Detail = $"'{match.GenericName}' added to current list",
                    Duration = 3000
                });
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Failed to add item. Please try again.",
                    Duration = 5000
                });
            }
        }
        finally
        {
            _isResolvingMatch = false;
            StateHasChanged();
        }
    }

    private void NavigateToBill(Guid billId)
    {
        DialogService.Close(true);
        NavigationManager.NavigateTo($"/bills/{billId}");
    }

    private void Cancel() => DialogService.Close(false);
}
