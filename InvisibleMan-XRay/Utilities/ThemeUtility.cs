using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

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
            if (string.IsNullOrEmpty(name))
                return AppThemeMode.Dark;

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

        public static ApplicationTheme Resolve(AppThemeMode mode) => mode switch
        {
            AppThemeMode.Light => ApplicationTheme.Light,
            AppThemeMode.System => DetectSystemTheme(),
            _ => ApplicationTheme.Dark
        };

        /// <summary>
        /// Re-evaluates the current backdrop of every open window so the
        /// Mica material stays in sync with the freshly applied palette.
        /// </summary>
        public static void RefreshWindowBackdrops()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is FluentWindow fluentWindow)
                    fluentWindow.RefreshBackdrop();
            }
        }

        private static ApplicationTheme DetectSystemTheme()
        {
            try
            {
                return ApplicationThemeManager.GetMatchedSystemTheme();
            }
            catch
            {
                return ApplicationTheme.Dark;
            }
        }
    }
}
