using BlazorUI.Models.Users;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Connections;

public partial class AddFriendDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<UserDto> AvailableUsers { get; set; } = [];

    string? _selectedUserId;

    void Submit() => DialogService.Close(_selectedUserId);

    void Cancel() => DialogService.Close(null);
}
