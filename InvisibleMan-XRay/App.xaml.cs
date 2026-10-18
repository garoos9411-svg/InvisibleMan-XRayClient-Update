using System;
using System.Windows;
using Microsoft.Win32;

namespace InvisibleManXRay
{
    using Managers;
    using Handlers;
    using Factories;

    public partial class App : Application
    {
        private AppManager appManager;
        private WindowFactory windowFactory;

        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeAppManager();
            InitializeWindowFactory();
            InitializeMainWindow();
            InitializeNotifyIcon();
            HandlePipes();
            HandleExitingEvents();

            void InitializeAppManager()
            {
                appManager = new AppManager(e.Args);
                appManager.Initialize();
            }

            void InitializeWindowFactory()
            {
                windowFactory = appManager.WindowFactory;
            }

            void InitializeMainWindow()
            {
                MainWindow mainWindow = windowFactory.CreateMainWindow();
                mainWindow.Show();
            }

            void InitializeNotifyIcon()
            {
                appManager.HandlersManager.GetHandler<NotifyHandler>().InitializeNotifyIcon();
            }

            void HandlePipes()
            {
                if (IsThereAnyArg())
                    PipeManager.SignalThisApp(e.Args);

                PipeManager.ListenForPipes();
            }

            void HandleExitingEvents()
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                SystemEvents.SessionEnded += OnSessionEnded;
            }

            bool IsThereAnyArg() => e.Args.Length != 0;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            SystemEvents.SessionEnded -= OnSessionEnded;

            appManager?.HandlersManager?.GetHandler<NotifyHandler>()?.DisposeNotifyIcon();
            CleanupBeforeExit();

            base.OnExit(e);
        }

        private void OnProcessExit(object? sender, EventArgs e) => CleanupBeforeExit();

        private void OnSessionEnded(object sender, SessionEndedEventArgs e) => CleanupBeforeExit();

        private void CleanupBeforeExit()
        {
            appManager?.Core.DisableMode();
        }
    }
}
