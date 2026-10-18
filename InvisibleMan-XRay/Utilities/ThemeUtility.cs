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
        /// Applies the resolved palette across the whole application. The
        /// Mica/Acrylic backdrops are kept in sync automatically by the
        /// <see cref="Wpf.Ui.Appearance.SystemThemeWatcher"/> registered for
        /// every <see cref="FluentWindow"/> (see MainWindow), so no manual
        /// backdrop refresh is required here.
        /// </summary>
        public static void Apply(AppThemeMode mode)
        {
            ApplicationTheme theme = Resolve(mode);

            // WPF-UI 3.1.1 exposes the overload with forceBackground, not
            // the older updateWpfuiTheme flag used in newer examples.
            ApplicationThemeManager.Apply(theme, forceBackground: false);
        }

        private static ApplicationTheme DetectSystemTheme()
        {
            try
            {
                SystemTheme systemTheme = ApplicationThemeManager.GetSystemTheme();
                return systemTheme == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
            }
            catch
            {
                return ApplicationTheme.Dark;
            }
        }
    }
}
