using BlazorUI.Models.Common;
using BlazorUI.Models.Users;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin;

public partial class UserDataGrid
{
    [Parameter, EditorRequired]
    public PaginatedList<UserDto> Data { get; set; } = new();

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public EventCallback<LoadDataArgs> OnLoadData { get; set; }

    [Parameter]
    public EventCallback<UserDto> OnRowSelect { get; set; }

    [Parameter]
    public EventCallback<UserDto> OnToggleActive { get; set; }

    int Count => Data.TotalCount;

    IEnumerable<UserDto> Items => Data.Items;

    async Task RowSelectAsync(UserDto user) => await OnRowSelect.InvokeAsync(user);
}
