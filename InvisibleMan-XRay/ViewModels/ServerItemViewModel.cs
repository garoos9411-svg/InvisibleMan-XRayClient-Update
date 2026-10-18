using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvisibleManXRay.ViewModels
{
    using Services;
    using Values;

    public partial class ServerItemViewModel : ObservableObject
    {
        private readonly Func<ServerItemViewModel, Task> testRequested;

        public string Path { get; }
        public string Name { get; }
        public string Group { get; }
        public bool HasGroup => !string.IsNullOrEmpty(Group);
        public string CountryCode { get; }
        public bool HasCountry => CountryCode.Length > 0;

        [ObservableProperty]
        private bool isCurrent;

        [ObservableProperty]
        private bool isTesting;

        [ObservableProperty]
        private ServerPingState pingState = ServerPingState.NotChecked;

        [ObservableProperty]
        private string pingText = "";

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public ServerItemViewModel(string path, string name, string group, Func<ServerItemViewModel, Task> testRequested)
        {
            Path = path;
            Name = GetDisplayName(name);
            Group = CleanName(group);
            this.testRequested = testRequested;
            CountryCode = ExtractCountryCode(name);
        }

        [RelayCommand(CanExecute = nameof(CanTest))]
        private Task TestAsync() => testRequested(this);

        private bool CanTest() => !IsTesting;

        partial void OnIsTestingChanged(bool value) => TestCommand.NotifyCanExecuteChanged();

        public void SetLatency(int latency)
        {
            if (latency == Availability.TIMEOUT)
            {
                PingState = ServerPingState.Bad;
                PingText = LocalizationService.GetTerm(Localization.SERVERS_PING_TIMEOUT);
            }
            else if (latency == Availability.ERROR)
            {
                PingState = ServerPingState.Bad;
                PingText = LocalizationService.GetTerm(Localization.SERVERS_PING_ERROR);
            }
            else
            {
                PingState = latency < 150 ? ServerPingState.Good
                    : latency < 400 ? ServerPingState.Medium
                    : ServerPingState.Bad;
                PingText = $"{latency} ms";
            }
        }

        private static string GetDisplayName(string name)
        {
            string clean = CleanName(name);

            if (clean.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(0, clean.Length - 5);

            return clean.TrimEnd();
        }

        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            StringBuilder clean = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                bool isRegionalIndicator = c == '\uD83C' || (c >= '\uDDE6' && c <= '\uDDFF');
                if (!isRegionalIndicator)
                    clean.Append(c);
            }

            return clean.ToString().Trim();
        }

        private static string ExtractCountryCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            StringBuilder code = new StringBuilder();
            for (int i = 0; i < name.Length - 1; i++)
            {
                if (name[i] == '\uD83C' && name[i + 1] >= '\uDDE6' && name[i + 1] <= '\uDDFF')
                {
                    code.Append((char)('A' + name[i + 1] - '\uDDE6'));
                    i++;
                }
            }

            return code.ToString();
        }
    }
}
