using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace InvisibleManXRay
{
    using System.Windows;
    using Models;
    using Services;
    using Services.Analytics.General;
    using Services.Analytics.MainWindow;
    using Values;
    using ViewModels;
    using Views.Pages;

    public partial class MainWindow : FluentWindow
    {
        private Func<bool> isNeedToShowPolicyWindow;
        private Func<bool> shouldStartHidden;
        private Func<bool> isNeedToAutoConnect;
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<Status> checkForBroadcast;
        private Func<ServerWindow> openServerWindow;
        private Func<SettingsWindow> openSettingsWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Func<PolicyWindow> openPolicyWindow;
        private Action<string> onRunServer;
        private Action onCancelServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGenerateClientId;
        private Action onGitHubClick;
        private Action onBugReportingClick;
        private Action<string> onCustomLinkClick;
        private Func<List<Config>> getAllGeneralConfigs;
        private Func<string, List<Config>> getAllSubscriptionConfigs;
        private Func<List<Subscription>> getAllGroups;
        private Func<string> getCurrentConfigPath;
        private Func<string, Status> loadConfigAt;
        private Func<string, int> testConnection;
        private Action<string> onUpdateConfigPath;
        private Action<ConnectionState> onConnectionStateChanged;

        private ProfileViewModel profileViewModel;
        private ServersViewModel serversViewModel;
        private ProfilePage currentProfilePage;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public MainWindow()
        {
            InitializeComponent();
            SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
            Loaded += OnShellLoaded;
        }

        public void Setup(
            Func<bool> isNeedToShowPolicyWindow,
            Func<bool> shouldStartHidden,
            Func<bool> isNeedToAutoConnect,
            Func<Config> getConfig,
            Func<Status> loadConfig,
            Func<Status> enableMode,
            Func<Status> checkForUpdate,
            Func<Status> checkForBroadcast,
            Func<ServerWindow> openServerWindow,
            Func<SettingsWindow> openSettingsWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Func<PolicyWindow> openPolicyWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onCancelServer,
            Action onDisableMode,
            Action onGenerateClientId,
            Action onGitHubClick,
            Action onBugReportingClick,
            Action<string> onCustomLinkClick,
            Func<List<Config>> getAllGeneralConfigs,
            Func<string, List<Config>> getAllSubscriptionConfigs,
            Func<List<Subscription>> getAllGroups,
            Func<string> getCurrentConfigPath,
            Func<string, Status> loadConfigAt,
            Func<string, int> testConnection,
            Action<string> onUpdateConfigPath,
            Action<ConnectionState> onConnectionStateChanged)
        {
            this.isNeedToShowPolicyWindow = isNeedToShowPolicyWindow;
            this.shouldStartHidden = shouldStartHidden;
            this.isNeedToAutoConnect = isNeedToAutoConnect;
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.enableMode = enableMode;
            this.checkForUpdate = checkForUpdate;
            this.checkForBroadcast = checkForBroadcast;
            this.openServerWindow = openServerWindow;
            this.openSettingsWindow = openSettingsWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.openPolicyWindow = openPolicyWindow;
            this.onRunServer = onRunServer;
            this.onCancelServer = onCancelServer;
            this.onStopServer = onStopServer;
            this.onDisableMode = onDisableMode;
            this.onGenerateClientId = onGenerateClientId;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;
            this.onCustomLinkClick = onCustomLinkClick;
            this.getAllGeneralConfigs = getAllGeneralConfigs;
            this.getAllSubscriptionConfigs = getAllSubscriptionConfigs;
            this.getAllGroups = getAllGroups;
            this.getCurrentConfigPath = getCurrentConfigPath;
            this.loadConfigAt = loadConfigAt;
            this.testConnection = testConnection;
            this.onUpdateConfigPath = onUpdateConfigPath;
            this.onConnectionStateChanged = onConnectionStateChanged;

            profileViewModel = new ProfileViewModel();
            profileViewModel.Setup(
                loadConfig: loadConfig,
                enableMode: enableMode,
                onRunServer: onRunServer,
                onStopServer: onStopServer,
                onCancelServer: onCancelServer,
                onDisableMode: onDisableMode
            );
            profileViewModel.ConfigLoadFailed += OnConfigLoadFailed;
            profileViewModel.ModeEnableFailed += OnModeEnableFailed;
            profileViewModel.PropertyChanged += OnProfilePropertyChanged;
            onConnectionStateChanged?.Invoke(profileViewModel.State);

            serversViewModel = new ServersViewModel();
            serversViewModel.Setup(
                getAllGeneralConfigs: getAllGeneralConfigs,
                getAllSubscriptionConfigs: getAllSubscriptionConfigs,
                getAllGroups: getAllGroups,
                getCurrentConfigPath: getCurrentConfigPath,
                loadConfigAt: loadConfigAt,
                testConnection: testConnection
            );
            serversViewModel.ServerSelected += OnServerSelected;

            UpdateUI();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            TryOpenPolicyWindow();
            TryStartHidden();
            TryAutoConnect();
            AnalyticsService.SendEvent(new AppOpenedEvent());
        }

        public void UpdateUI()
        {
            profileViewModel?.UpdateCurrentServer(GetCurrentServerName());
        }

        public void TryRerun()
        {
            profileViewModel?.TryRerun();
        }

        public void TryDisableModeAndRerun()
        {
            profileViewModel?.TryDisableModeAndRerun();
        }

        private void OnShellLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnShellLoaded;

            if (RootNavigation.SelectedItem == null)
                RootNavigation.Navigate(typeof(ProfilePage));
        }

        private void OnNavigationNavigated(NavigationView sender, NavigatedEventArgs args)
        {
            currentProfilePage = args.Page as ProfilePage;
            if (currentProfilePage != null)
            {
                currentProfilePage.DataContext = profileViewModel;
                currentProfilePage.ManageServersRequested += OnManageServersRequested;
            }

            if (args.Page is ServersPage serversPage)
            {
                serversPage.DataContext = serversViewModel;
                serversPage.OpenServerManagerRequested += OnManageServersRequested;
                serversViewModel.RefreshServers();
                _ = serversViewModel.TestPendingServersAsync();
            }

            if (args.Page is SettingsPage settingsPage)
            {
                settingsPage.OpenSettingsRequested += OnOpenSettingsRequested;
                settingsPage.OpenUpdateRequested += OnOpenUpdateRequested;
                settingsPage.OpenAboutRequested += OnOpenAboutRequested;
                settingsPage.GitHubRequested += OnGitHubRequested;
                settingsPage.BugReportingRequested += OnBugReportingRequested;
            }
        }

        private string GetCurrentServerName() => getConfig?.Invoke()?.Name;

        private void OnManageServersRequested(object? sender, EventArgs e) => OpenServerWindow();

        private void OnServerSelected(object? sender, string configPath) => onUpdateConfigPath?.Invoke(configPath);

        private void OnOpenSettingsRequested(object? sender, EventArgs e) => OpenSettingsWindow();

        private void OnOpenUpdateRequested(object? sender, EventArgs e) => OpenUpdateWindow();

        private void OnOpenAboutRequested(object? sender, EventArgs e) => OpenAboutWindow();

        private void OnGitHubRequested(object? sender, EventArgs e) => OpenGitHub();

        private void OnBugReportingRequested(object? sender, EventArgs e) => OpenBugReporting();

        private void OnGitHubButtonClick(object sender, RoutedEventArgs e) => OpenGitHub();

        private void OnBugReportingButtonClick(object sender, RoutedEventArgs e) => OpenBugReporting();

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e) => OpenUpdateWindow();

        private void OnAboutButtonClick(object sender, RoutedEventArgs e) => OpenAboutWindow();

        private void OpenGitHub()
        {
            onGitHubClick?.Invoke();
            AnalyticsService.SendEvent(new GitHubButtonClickedEvent());
        }

        private void OpenBugReporting()
        {
            onBugReportingClick?.Invoke();
            AnalyticsService.SendEvent(new BugReportingButtonClickedEvent());
        }

        private void OpenServerWindow()
        {
            if (openServerWindow == null)
                return;

            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();

            UpdateUI();
            AnalyticsService.SendEvent(new ManageServersButtonClickedEvent());
        }

        private void OpenSettingsWindow()
        {
            if (openSettingsWindow == null)
                return;

            SettingsWindow settingsWindow = openSettingsWindow.Invoke();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            UpdateUI();
            AnalyticsService.SendEvent(new SettingsButtonClickedEvent());
        }

        private void OpenUpdateWindow()
        {
            if (openUpdateWindow == null)
                return;

            UpdateWindow updateWindow = openUpdateWindow.Invoke();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();

            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        private void OpenAboutWindow()
        {
            if (openAboutWindow == null)
                return;

            AboutWindow aboutWindow = openAboutWindow.Invoke();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();

            AnalyticsService.SendEvent(new AboutButtonClickedEvent());
        }

        private void TryOpenPolicyWindow()
        {
            if (!(isNeedToShowPolicyWindow?.Invoke() ?? false))
                return;

            onGenerateClientId?.Invoke();
            AnalyticsService.SendEvent(new NewUserEvent());

            PolicyWindow policyWindow = openPolicyWindow.Invoke();
            policyWindow.Owner = this;
            policyWindow.ShowDialog();
        }

        private void TryStartHidden()
        {
            if (!(shouldStartHidden?.Invoke() ?? false))
                return;

            if (Application.Current.Windows.Count > 1)
                return;

            OnClosing(new CancelEventArgs());
        }

        private void TryAutoConnect()
        {
            if (!(isNeedToAutoConnect?.Invoke() ?? false))
                return;

            if (profileViewModel == null)
                return;

            if (profileViewModel.ConnectCommand.CanExecute(null))
                profileViewModel.ConnectCommand.Execute(null);
        }

        private void OnConfigLoadFailed(object? sender, StatusEventArgs e)
        {
            Status status = e.Status;

            if (IsAnotherWindowOpened())
                return;

            ForceShowWindowIfNeeded();

            switch (status.SubCode)
            {
                case SubCode.NO_CONFIG:
                    HandleNoConfigError();
                    break;
                case SubCode.INVALID_CONFIG:
                    HandleInvalidConfigError();
                    break;
                default:
                    return;
            }

            void HandleNoConfigError()
            {
                MessageBoxResult result = MessageBox.Show(
                    this,
                    status.Content.ToString(),
                    Caption.WARNING,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.OK)
                    OpenServerWindow();
            }

            void HandleInvalidConfigError()
            {
                MessageBox.Show(
                    this,
                    status.Content.ToString(),
                    Caption.ERROR,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void OnModeEnableFailed(object? sender, StatusEventArgs e)
        {
            MessageBox.Show(
                this,
                e.Status.Content.ToString(),
                Caption.ERROR,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProfileViewModel.State))
                onConnectionStateChanged?.Invoke(profileViewModel.State);
        }

        private void ForceShowWindowIfNeeded()
        {
            if (Visibility != Visibility.Hidden)
                return;

            Show();
        }

        private bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
