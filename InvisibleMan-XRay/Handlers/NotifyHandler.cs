using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Tray;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Services;
    using Services.Analytics.Notify;
    using Utilities;
    using Values;

    public class NotifyHandler : Handler
    {
        private const int ConnectingFrameCount = 8;
        private const int ConnectingFrameIntervalMilliseconds = 120;

        private TrayIconService notifyIcon;
        private Dictionary<Mode, MenuItem> modeItems;

        private ConnectionState connectionState = ConnectionState.Disconnected;
        private DispatcherTimer connectingTimer;
        private ImageSource disconnectedIcon;
        private ImageSource connectedIcon;
        private ImageSource[] connectingFrames;
        private int connectingFrameIndex;

        private Func<Mode> getMode;
        private Action onOpenClick;
        private Action onUpdateClick;
        private Action onAboutClick;
        private Action onCloseClick;
        private Action onProxyModeClick;
        private Action onTunnelModeClick;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public void Setup(
            Func<Mode> getMode,
            Action onOpenClick,
            Action onUpdateClick,
            Action onAboutClick,
            Action onCloseClick,
            Action onProxyModeClick,
            Action onTunnelModeClick
        )
        {
            this.getMode = getMode;
            this.onOpenClick = onOpenClick;
            this.onUpdateClick = onUpdateClick;
            this.onAboutClick = onAboutClick;
            this.onCloseClick = onCloseClick;
            this.onProxyModeClick = onProxyModeClick;
            this.onTunnelModeClick = onTunnelModeClick;
        }

        public void CheckModeItem(Mode mode)
        {
            if (modeItems == null || !modeItems.TryGetValue(mode, out MenuItem modeItem))
                return;

            UncheckAllItems();
            modeItem.IsChecked = true;
        }

        public void SetConnectionState(ConnectionState state)
        {
            connectionState = state;
            UpdateIcon();
        }

        public void InitializeNotifyIcon()
        {
            DisposeNotifyIcon();

            modeItems = new Dictionary<Mode, MenuItem>();
            notifyIcon = new TrayIconService(onOpenClick)
            {
                TooltipText = "Invisible Man XRay",
                ContextMenu = CreateContextMenu()
            };
            notifyIcon.Register();
            UpdateIcon();
        }

        public void DisposeNotifyIcon()
        {
            if (notifyIcon == null)
                return;

            StopConnectingAnimation();
            notifyIcon.Unregister();
            notifyIcon = null;
            modeItems = null;
        }

        private void UpdateIcon()
        {
            if (notifyIcon == null)
                return;

            EnsureIcons();

            switch (connectionState)
            {
                case ConnectionState.Connected:
                    StopConnectingAnimation();
                    notifyIcon.Icon = connectedIcon;
                    break;
                case ConnectionState.Connecting:
                    StartConnectingAnimation();
                    break;
                default:
                    StopConnectingAnimation();
                    notifyIcon.Icon = disconnectedIcon;
                    break;
            }

            notifyIcon.TooltipText = $"Invisible Man XRay — {GetStateTerm()}";

            string GetStateTerm()
            {
                switch (connectionState)
                {
                    case ConnectionState.Connected:
                        return LocalizationService.GetTerm(Localization.NOTIFY_STATE_CONNECTED);
                    case ConnectionState.Connecting:
                        return LocalizationService.GetTerm(Localization.NOTIFY_STATE_CONNECTING);
                    default:
                        return LocalizationService.GetTerm(Localization.NOTIFY_STATE_DISCONNECTED);
                }
            }
        }

        private void EnsureIcons()
        {
            if (disconnectedIcon != null)
                return;

            disconnectedIcon = TrayIconFactory.CreateDisconnectedIcon();
            connectedIcon = TrayIconFactory.CreateConnectedIcon();
            connectingFrames = TrayIconFactory.CreateConnectingFrames(ConnectingFrameCount);
        }

        private void StartConnectingAnimation()
        {
            if (connectingTimer == null)
            {
                connectingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(ConnectingFrameIntervalMilliseconds)
                };
                connectingTimer.Tick += OnConnectingTimerTick;
            }

            connectingFrameIndex = 0;
            notifyIcon.Icon = connectingFrames[0];

            if (!connectingTimer.IsEnabled)
                connectingTimer.Start();
        }

        private void StopConnectingAnimation()
        {
            if (connectingTimer == null)
                return;

            connectingTimer.Stop();
        }

        private void OnConnectingTimerTick(object? sender, EventArgs e)
        {
            if (notifyIcon == null || connectingFrames == null)
                return;

            notifyIcon.Icon = connectingFrames[connectingFrameIndex];
            connectingFrameIndex = (connectingFrameIndex + 1) % connectingFrames.Length;
        }

        private ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateItem(LocalizationService.GetTerm(Localization.NOTIFY_OPEN), OnOpenClick));

            MenuItem modeMenu = CreateItem(LocalizationService.GetTerm(Localization.NOTIFY_MODE), onClick: null);
            modeItems[Mode.PROXY] = CreateModeItem("Proxy", OnProxyModeClick, getMode.Invoke() == Mode.PROXY);
            modeItems[Mode.TUN] = CreateModeItem("TUN", OnTunnelModeClick, getMode.Invoke() == Mode.TUN);
            foreach (MenuItem modeItem in modeItems.Values)
                modeMenu.Items.Add(modeItem);
            contextMenu.Items.Add(modeMenu);

            contextMenu.Items.Add(CreateItem(LocalizationService.GetTerm(Localization.NOTIFY_UPDATE), OnUpdateClick));
            contextMenu.Items.Add(CreateItem(LocalizationService.GetTerm(Localization.NOTIFY_ABOUT), OnAboutClick));
            contextMenu.Items.Add(CreateItem(LocalizationService.GetTerm(Localization.NOTIFY_CLOSE), OnCloseClick));

            return contextMenu;
        }

        private MenuItem CreateItem(string text, Action onClick)
        {
            MenuItem item = new MenuItem() { Header = text };
            if (onClick != null)
                item.Click += (sender, e) => onClick.Invoke();

            return item;
        }

        private MenuItem CreateModeItem(string text, Action onClick, bool isChecked)
        {
            MenuItem item = new MenuItem() { Header = text, IsCheckable = true, IsChecked = isChecked };
            item.Click += (sender, e) =>
            {
                UncheckAllItems();
                item.IsChecked = true;
                onClick.Invoke();
            };

            return item;
        }

        private void UncheckAllItems()
        {
            foreach (MenuItem item in modeItems.Values)
                item.IsChecked = false;
        }

        private void OnOpenClick()
        {
            AnalyticsService.SendEvent(new OpenClickedEvent());
            onOpenClick.Invoke();
        }

        private void OnUpdateClick()
        {
            AnalyticsService.SendEvent(new CheckForUpdateClickedEvent());
            onUpdateClick.Invoke();
        }

        private void OnAboutClick()
        {
            AnalyticsService.SendEvent(new AboutClickedEvent());
            onAboutClick.Invoke();
        }

        private void OnProxyModeClick()
        {
            AnalyticsService.SendEvent(new ProxyModeClickedEvent());
            onProxyModeClick.Invoke();
        }

        private void OnTunnelModeClick()
        {
            AnalyticsService.SendEvent(new TunModeClickedEvent());
            onTunnelModeClick.Invoke();
        }

        private void OnCloseClick()
        {
            onCloseClick.Invoke();
        }

        private sealed class TrayIconService : NotifyIconService
        {
            private readonly Action onLeftClick;

            public TrayIconService(Action onLeftClick)
            {
                this.onLeftClick = onLeftClick;
            }

            protected override void OnLeftClick()
            {
                onLeftClick?.Invoke();
            }
        }
    }
}
