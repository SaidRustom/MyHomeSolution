using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class UserAvatar : IDisposable
{
    [Inject]
    IAvatarService AvatarService { get; set; } = default!;

    [Parameter]
    public string? UserId { get; set; }

    [Parameter]
    public string? AvatarUrl { get; set; }

    [Parameter]
    public string? FallbackName { get; set; }

    [Parameter]
    public string FallbackIcon { get; set; } = "person";

    [Parameter]
    public string Size { get; set; } = "32px";

    [Parameter]
    public string Background { get; set; } = "var(--rz-primary-lighter)";

    [Parameter]
    public string IconColor { get; set; } = "var(--rz-primary)";

    string SizePx => Size;
    string IconSize => ParseSize(Size) switch
    {
        <= 24 => "0.7rem",
        <= 32 => "0.875rem",
        <= 40 => "1rem",
        _ => "1.125rem"
    };

    string? _dataUrl;
    string? _lastUserId;
    CancellationTokenSource _cts = new();

    protected override async Task OnParametersSetAsync()
    {
        if (_lastUserId == UserId)
            return;

        _lastUserId = UserId;

        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(AvatarUrl))
        {
            _dataUrl = null;
            return;
        }

        _dataUrl = await AvatarService.GetAvatarDataUrlAsync(UserId, _cts.Token);
    }

    static int ParseSize(string size)
    {
        var numeric = new string(size.Where(char.IsDigit).ToArray());
        return int.TryParse(numeric, out var n) ? n : 32;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
