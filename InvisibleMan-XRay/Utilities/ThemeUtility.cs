using System;
using Wpf.Ui.Appearance;

namespace InvisibleManXRay.Utilities
{
    using ViewModels;

    public static class ThemeUtility
    {
        public const string DARK = "Dark";
        public const string LIGHT = "Light";
        public const string SYSTEM = "System";

        public static AppThemeMode Parse(string name)
        {
            if (Enum.TryParse(name, ignoreCase: true, out AppThemeMode mode))
                return mode;

            return AppThemeMode.Dark;
        }

        public static string ToSettingName(AppThemeMode mode) => mode switch
        {
            AppThemeMode.Light => LIGHT,
            AppThemeMode.System => SYSTEM,
            _ => DARK
        };

        public static void Apply(AppThemeMode mode)
        {
            ApplicationTheme theme = mode switch
            {
                AppThemeMode.Light => ApplicationTheme.Light,
                AppThemeMode.System => DetectSystemTheme(),
                _ => ApplicationTheme.Dark
            };

            ThemeManagement.Apply(theme, BackgroundType.Mica);
        }

        private static ApplicationTheme DetectSystemTheme()
        {
            try
            {
                return ThemeManagement.GetActualTheme();
            }
            catch
            {
                return ApplicationTheme.Dark;
            }
        }
    }
}
