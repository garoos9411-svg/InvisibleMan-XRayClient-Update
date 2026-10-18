using System;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public event EventHandler OpenSettingsRequested;
        public event EventHandler OpenUpdateRequested;
        public event EventHandler OpenAboutRequested;
        public event EventHandler GitHubRequested;
        public event EventHandler BugReportingRequested;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnOpenUpdateClick(object sender, RoutedEventArgs e)
        {
            OpenUpdateRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnOpenAboutClick(object sender, RoutedEventArgs e)
        {
            OpenAboutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            GitHubRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnBugReportingClick(object sender, RoutedEventArgs e)
        {
            BugReportingRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
