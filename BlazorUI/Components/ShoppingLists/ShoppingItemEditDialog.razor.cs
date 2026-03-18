using BlazorUI.Models.Common;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.ShoppingLists;

public partial class ShoppingItemEditDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public ShoppingItemDto Item { get; set; } = default!;

    [Parameter]
    public Guid ListId { get; set; }

    string? _name;
    int _quantity;
    string? _unit;
    string? _notes;
    bool _isBusy;
    string? _errorMessage;

    protected override void OnInitialized()
    {
        _name = Item.Name;
        _quantity = Item.Quantity;
        _unit = Item.Unit;
        _notes = Item.Notes;
    }

    async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name))
            return;

        _isBusy = true;
        _errorMessage = null;

        var request = new UpdateShoppingItemRequest
        {
            ShoppingListId = ListId,
            ItemId = Item.Id,
            Name = _name.Trim(),
            Quantity = _quantity,
            Unit = string.IsNullOrWhiteSpace(_unit) ? null : _unit.Trim(),
            Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim(),
            SortOrder = Item.SortOrder
        };

        var result = await ShoppingListService.UpdateItemAsync(ListId, Item.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Item Updated",
                Duration = 3000
            });
            DialogService.Close(true);
        }
        else
        {
            _errorMessage = result.Problem.ToUserMessage();
        }

        _isBusy = false;
    }

    void Cancel()
    {
        DialogService.Close(false);
    }
}
