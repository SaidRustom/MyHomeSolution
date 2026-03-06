using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Settings
{
    public partial class ThemeSelector : IDisposable
    {
        [Inject]
        ThemeService ThemeService { get; set; } = default!;

        string _currentTheme { get; set; } = default!;

        protected override void OnInitialized()
        {
            _currentTheme = ThemeService.Theme;
            ThemeService.ThemeChanged += OnThemeChanged;
        }

        void ChangeTheme(string value)
        {
            ThemeService.SetTheme(value);
        }

        void OnThemeChanged()
        {
            _currentTheme = ThemeService.Theme;
            StateHasChanged();
        }

        public void Dispose()
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }
    }
}
