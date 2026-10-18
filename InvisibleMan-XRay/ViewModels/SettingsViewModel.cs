using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvisibleManXRay.ViewModels
{
    using Utilities;

    public partial class SettingsViewModel : ObservableObject
    {
        private Func<string> getTheme;
        private Action<string> onThemeChanged;

        [ObservableProperty]
        private AppThemeMode themeMode = AppThemeMode.Dark;

        public void Setup(Func<string> getTheme, Action<string> onThemeChanged)
        {
            this.getTheme = getTheme;
            this.onThemeChanged = onThemeChanged;

            // Set the backing field directly to avoid triggering OnThemeModeChanged
            // (and therefore ThemeUtility.Apply + onThemeChanged) during initialization.
            themeMode = getTheme != null
                ? ThemeUtility.Parse(getTheme.Invoke())
                : AppThemeMode.Dark;
            OnPropertyChanged(nameof(ThemeMode));
        }

        partial void OnThemeModeChanged(AppThemeMode value)
        {
            ThemeUtility.Apply(value);
            onThemeChanged?.Invoke(ThemeUtility.ToSettingName(value));
        }

        [RelayCommand]
        private void SelectDarkTheme() => ThemeMode = AppThemeMode.Dark;

        [RelayCommand]
        private void SelectLightTheme() => ThemeMode = AppThemeMode.Light;

        [RelayCommand]
        private void SelectSystemTheme() => ThemeMode = AppThemeMode.System;
    }
}
