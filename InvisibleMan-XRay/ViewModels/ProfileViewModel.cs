using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvisibleManXRay.ViewModels
{
    using Models;
    using Services;
    using Services.Analytics.General;
    using Services.Analytics.MainWindow;
    using Values;

    public partial class ProfileViewModel : ObservableObject
    {
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Action<string> onRunServer;
        private Action onStopServer;
        private Action onCancelServer;
        private Action onDisableMode;

        private bool rerunRequest;

        public event EventHandler<StatusEventArgs> ConfigLoadFailed;
        public event EventHandler<StatusEventArgs> ModeEnableFailed;

        public TrafficViewModel Traffic { get; } = new TrafficViewModel();

        [ObservableProperty]
        private ConnectionState state = ConnectionState.Disconnected;

        [ObservableProperty]
        private string currentServerName = "";

        public string CurrentServerDisplay =>
            string.IsNullOrWhiteSpace(CurrentServerName)
                ? LocalizationService.GetTerm(Localization.NO_SERVER_CONFIGURATION)
                : CurrentServerName;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        partial void OnStateChanged(ConnectionState value)
        {
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();

            if (value == ConnectionState.Connected)
                Traffic.Start();
            else
                Traffic.Stop();
        }

        public void Setup(
            Func<Status> loadConfig,
            Func<Status> enableMode,
            Action<string> onRunServer,
            Action onStopServer,
            Action onCancelServer,
            Action onDisableMode)
        {
            this.loadConfig = loadConfig;
            this.enableMode = enableMode;
            this.onRunServer = onRunServer;
            this.onStopServer = onStopServer;
            this.onCancelServer = onCancelServer;
            this.onDisableMode = onDisableMode;
        }

        public void UpdateCurrentServer(string name)
        {
            CurrentServerName = name ?? "";
            OnPropertyChanged(nameof(CurrentServerDisplay));
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            AnalyticsService.SendEvent(new RunButtonClickedEvent());

            State = ConnectionState.Connecting;

            Status configStatus = await Task.Run(loadConfig);

            if (configStatus.Code == Code.ERROR)
            {
                State = ConnectionState.Disconnected;
                ConfigLoadFailed?.Invoke(this, new StatusEventArgs(configStatus));
                return;
            }

            Status modeStatus = await Task.Run(enableMode);

            if (modeStatus.Code == Code.ERROR)
            {
                State = ConnectionState.Disconnected;
                ModeEnableFailed?.Invoke(this, new StatusEventArgs(modeStatus));
                return;
            }

            if (modeStatus.Code == Code.INFO && modeStatus.SubCode == SubCode.CANCELED)
            {
                State = ConnectionState.Disconnected;
                return;
            }

            State = ConnectionState.Connected;

            await Task.Run(() => onRunServer(configStatus.Content.ToString()));

            State = ConnectionState.Disconnected;

            if (rerunRequest)
            {
                rerunRequest = false;
                await ConnectAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private async Task DisconnectAsync()
        {
            AnalyticsService.SendEvent(new StopButtonClickedEvent());

            rerunRequest = false;
            State = ConnectionState.Disconnected;

            await Task.Run(() => {
                onStopServer();
                onDisableMode();
            });
        }

        [RelayCommand]
        private void Cancel()
        {
            rerunRequest = false;
            onCancelServer();
        }

        public void TryRerun()
        {
            if (State == ConnectionState.Disconnected)
                return;

            rerunRequest = true;
            Task.Run(onStopServer);
        }

        public void TryDisableModeAndRerun()
        {
            if (State == ConnectionState.Disconnected)
                return;

            rerunRequest = true;
            Task.Run(() => {
                onDisableMode();
                onStopServer();
            });
        }

        private bool CanConnect() => State == ConnectionState.Disconnected;

        private bool CanDisconnect() => State == ConnectionState.Connected;
    }
}
