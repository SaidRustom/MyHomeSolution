using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorUI.Pages.Portfolio;

public partial class Portfolio : IAsyncDisposable
{
    [Inject] private IPortfolioService PortfolioService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private PortfolioDto? _portfolio;
    private bool _isLoading = true;
    private bool _scrolled;
    private bool _menuOpen;
    private DotNetObjectReference<Portfolio>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        var result = await PortfolioService.GetPortfolioAsync();

        if (result.IsSuccess)
            _portfolio = result.Value;

        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("portfolioScrollInit", _dotNetRef);
        }
    }

    [JSInvokable]
    public void OnScroll(bool scrolled)
    {
        if (_scrolled != scrolled)
        {
            _scrolled = scrolled;
            InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleMenu() => _menuOpen = !_menuOpen;

    private void CloseMenu() => _menuOpen = false;

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            try
            {
                await JS.InvokeVoidAsync("portfolioScrollDestroy");
            }
            catch (JSDisconnectedException) { }
            _dotNetRef.Dispose();
        }
    }
}
