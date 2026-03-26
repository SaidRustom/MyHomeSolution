using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorUI.Components.Demo;

public partial class DemoCountdownBanner : ComponentBase, IDisposable
{
    [Inject] private IDemoService DemoService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool IsDemoUser { get; set; }
    private DateTimeOffset ExpiresAt { get; set; }
    private TimeSpan TimeRemaining { get; set; }

    private Timer? _countdownTimer;
    private bool _isLoaded;

    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthState;
        if (state.User.Identity?.IsAuthenticated != true)
        {
            IsDemoUser = false;
            StopTimer();
            return;
        }

        // Only fetch status once per component lifetime to avoid spamming the API
        if (_isLoaded) return;
        _isLoaded = true;

        try
        {
            var result = await DemoService.GetDemoStatusAsync();
            if (result.IsSuccess && result.Value.IsDemoUser)
            {
                IsDemoUser = true;
                ExpiresAt = result.Value.ExpiresAt!.Value;
                UpdateTimeRemaining();
                StartTimer();
            }
        }
        catch
        {
            // Non-critical; fail silently
        }
    }

    private void StartTimer()
    {
        StopTimer();
        _countdownTimer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StopTimer()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void OnTimerTick(object? state)
    {
        UpdateTimeRemaining();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateTimeRemaining()
    {
        var remaining = ExpiresAt - DateTimeOffset.UtcNow;
        TimeRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private string FormatTimeRemaining()
    {
        if (TimeRemaining <= TimeSpan.Zero)
            return "00:00:00";

        var hours = (int)TimeRemaining.TotalHours;
        return $"{hours:D2}:{TimeRemaining.Minutes:D2}:{TimeRemaining.Seconds:D2}";
    }

    public void Dispose()
    {
        StopTimer();
    }
}
