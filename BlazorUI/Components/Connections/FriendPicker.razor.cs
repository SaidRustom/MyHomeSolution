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

    /// <summary>
    /// User IDs to exclude from the dropdown (e.g. already-selected split users).
    /// </summary>
    [Parameter]
    public IEnumerable<string>? ExcludedUserIds { get; set; }

    /// <summary>
    /// When set, injects the current user as the first option in the dropdown.
    /// </summary>
    [Parameter]
    public UserDto? CurrentUser { get; set; }

    IEnumerable<UserDto> _allUsers = [];
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
            var users = result.Value.ToList();

            // Inject current user at the top if requested and not already present
            if (CurrentUser is not null && !users.Any(u => u.Id == CurrentUser.Id))
            {
                users.Insert(0, CurrentUser);
            }

            _allUsers = users;
            ApplyExclusions();
        }
    }

    void ApplyExclusions()
    {
        if (ExcludedUserIds is not null)
        {
            var excluded = ExcludedUserIds.ToHashSet();
            // Keep the currently selected user visible so the dropdown can display the selection
            _users = _allUsers.Where(u => !excluded.Contains(u.Id) || u.Id == SelectedUserId);
        }
        else
        {
            _users = _allUsers;
        }
    }

    protected override void OnParametersSet()
    {
        ApplyExclusions();
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
