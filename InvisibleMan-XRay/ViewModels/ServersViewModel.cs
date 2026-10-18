using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvisibleManXRay.ViewModels
{
    using Models;
    using Values;

    public partial class ServersViewModel : ObservableObject
    {
        private Func<List<Config>> getAllGeneralConfigs;
        private Func<string, List<Config>> getAllSubscriptionConfigs;
        private Func<List<Subscription>> getAllGroups;
        private Func<string> getCurrentConfigPath;
        private Func<string, Status> loadConfigAt;
        private Func<string, int> testConnection;

        private readonly Dictionary<string, Tuple<ServerPingState, string>> pingCache =
            new Dictionary<string, Tuple<ServerPingState, string>>();

        public event EventHandler<string> ServerSelected;

        public ObservableCollection<ServerItemViewModel> Servers { get; } =
            new ObservableCollection<ServerItemViewModel>();

        [ObservableProperty]
        private bool hasServers;

        public void Setup(
            Func<List<Config>> getAllGeneralConfigs,
            Func<string, List<Config>> getAllSubscriptionConfigs,
            Func<List<Subscription>> getAllGroups,
            Func<string> getCurrentConfigPath,
            Func<string, Status> loadConfigAt,
            Func<string, int> testConnection)
        {
            this.getAllGeneralConfigs = getAllGeneralConfigs;
            this.getAllSubscriptionConfigs = getAllSubscriptionConfigs;
            this.getAllGroups = getAllGroups;
            this.getCurrentConfigPath = getCurrentConfigPath;
            this.loadConfigAt = loadConfigAt;
            this.testConnection = testConnection;
        }

        public void RefreshServers()
        {
            string currentPath = Safe(getCurrentConfigPath) ?? "";

            List<ServerItemViewModel> items = new List<ServerItemViewModel>();

            List<Config> generalConfigs = (Safe(getAllGeneralConfigs) ?? new List<Config>()).ToList();
            foreach (Config config in generalConfigs)
                items.Add(CreateItem(config.Path, config.Name, "", currentPath));

            List<Subscription> groups = (Safe(getAllGroups) ?? new List<Subscription>()).ToList();
            foreach (Subscription group in groups)
            {
                if (group.Directory == null)
                    continue;

                List<Config> groupConfigs = (Safe(() => getAllSubscriptionConfigs(group.Directory.FullName)) ?? new List<Config>()).ToList();
                foreach (Config config in groupConfigs)
                    items.Add(CreateItem(config.Path, config.Name, group.Directory.Name, currentPath));
            }

            Servers.Clear();
            foreach (ServerItemViewModel item in items)
                Servers.Add(item);

            HasServers = Servers.Count > 0;
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            RefreshServers();
            await TestPendingServersAsync();
        }

        public async Task TestPendingServersAsync()
        {
            foreach (ServerItemViewModel item in Servers)
            {
                if (item.PingState != ServerPingState.NotChecked)
                    continue;

                await TestServerAsync(item);
            }
        }

        public async Task TestServerAsync(ServerItemViewModel item)
        {
            if (item == null || item.IsTesting)
                return;

            item.IsTesting = true;
            try
            {
                int latency = await Task.Run(() =>
                {
                    Status configStatus = loadConfigAt.Invoke(item.Path);

                    if (configStatus.Code == Code.ERROR)
                        return Availability.ERROR;

                    return testConnection.Invoke(configStatus.Content.ToString());
                });

                item.SetLatency(latency);
                pingCache[item.Path] = Tuple.Create(item.PingState, item.PingText);
            }
            catch
            {
                item.SetLatency(Availability.ERROR);
                pingCache[item.Path] = Tuple.Create(item.PingState, item.PingText);
            }
            finally
            {
                item.IsTesting = false;
            }
        }

        public void Select(ServerItemViewModel item)
        {
            if (item == null)
                return;

            foreach (ServerItemViewModel server in Servers)
                server.IsCurrent = server == item;

            ServerSelected?.Invoke(this, item.Path);
        }

        private ServerItemViewModel CreateItem(string path, string name, string group, string currentPath)
        {
            ServerItemViewModel item = new ServerItemViewModel(path, name, group, TestServerAsync);

            if (pingCache.TryGetValue(path, out Tuple<ServerPingState, string> cached))
            {
                item.PingState = cached.Item1;
                item.PingText = cached.Item2;
            }

            item.IsCurrent = path == currentPath;
            return item;
        }

        private T Safe<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch
            {
                return default;
            }
        }
    }
}
