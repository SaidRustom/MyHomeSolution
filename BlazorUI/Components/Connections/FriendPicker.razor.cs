using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Connections;

public partial class FriendPicker
{
    [Inject]
    IUserConnectionService UserConnectionService { get; set; } = default!;

    [Parameter]
    public string? SelectedUserId { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedUserIdChanged { get; set; }

    [Parameter]
    public IEnumerable<string>? SelectedUserIds { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<string>?> SelectedUserIdsChanged { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "Search friends…";

    [Parameter]
    public string? Style { get; set; } = "width: 100%;";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    IEnumerable<UserDto> _users = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync(null);
    }

    async Task OnLoadData(LoadDataArgs args)
    {
        await LoadUsersAsync(args.Filter);
    }

    async Task LoadUsersAsync(string? searchTerm)
    {
        var result = await UserConnectionService.SearchConnectedUsersAsync(
            searchTerm: searchTerm, maxResults: 50);

        if (result.IsSuccess)
        {
            _users = result.Value;
        }
    }

    async Task OnSingleValueChanged(string? value)
    {
        SelectedUserId = value;
        await SelectedUserIdChanged.InvokeAsync(value);
    }

    async Task OnMultiValueChanged(IEnumerable<string>? values)
    {
        SelectedUserIds = values;
        await SelectedUserIdsChanged.InvokeAsync(values);
    }
}
