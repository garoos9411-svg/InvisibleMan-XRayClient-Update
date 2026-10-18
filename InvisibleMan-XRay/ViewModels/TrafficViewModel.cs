using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace InvisibleManXRay.ViewModels
{
    using Services;
    using Values;

    public partial class TrafficViewModel : ObservableObject
    {
        private const int MaxPoints = 40;
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(500);

        private PeriodicTimer timer;
        private CancellationTokenSource cancellation;
        private long lastReceived;
        private long lastSent;
        private bool hasBaseline;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public ObservableCollection<double> DownloadPoints { get; } =
            new ObservableCollection<double>();

        public ObservableCollection<double> UploadPoints { get; } =
            new ObservableCollection<double>();

        [ObservableProperty]
        private ISeries[] series = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] xAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] yAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty]
        private string downloadSpeed = FormatSpeed(0);

        [ObservableProperty]
        private string uploadSpeed = FormatSpeed(0);

        [ObservableProperty]
        private double downloadValue;

        [ObservableProperty]
        private double uploadValue;

        public void Start()
        {
            if (IsActive)
                return;

            BuildChart();
            hasBaseline = false;
            cancellation = new CancellationTokenSource();
            timer = new PeriodicTimer(UpdateInterval);
            IsActive = true;
            _ = PollAsync(cancellation.Token);
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            IsActive = false;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            timer?.Dispose();
            timer = null;

            hasBaseline = false;
            OnStatsUpdated(0, 0, clear: true);
        }

        private void BuildChart()
        {
            SKColor downloadColor = ResolveColor("Accent.Traffic.Color");
            SKColor uploadColor = ResolveColor("Accent.Success.Color");

            Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = LocalizationService.GetTerm(Localization.PROFILE_DOWNLOAD),
                    Values = DownloadPoints,
                    Stroke = new SolidColorPaint(downloadColor, 2f),
                    Fill = new LinearGradientPaint(
                        downloadColor.WithAlpha(0x38),
                        downloadColor.WithAlpha(0x00),
                        new SKPoint(0.5f, 0f),
                        new SKPoint(0.5f, 1f)),
                    GeometrySize = 0,
                    LineSmoothness = 0.6
                },
                new LineSeries<double>
                {
                    Name = LocalizationService.GetTerm(Localization.PROFILE_UPLOAD),
                    Values = UploadPoints,
                    Stroke = new SolidColorPaint(uploadColor, 2f),
                    Fill = new LinearGradientPaint(
                        uploadColor.WithAlpha(0x2E),
                        uploadColor.WithAlpha(0x00),
                        new SKPoint(0.5f, 0f),
                        new SKPoint(0.5f, 1f)),
                    GeometrySize = 0,
                    LineSmoothness = 0.6
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    IsVisible = false,
                    MinLimit = 0,
                    MaxLimit = MaxPoints - 1
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    IsVisible = false,
                    MinLimit = 0
                }
            };
        }

        private async Task PollAsync(CancellationToken token)
        {
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    (long received, long sent) = GetInterfaceBytes();

                    double download = 0;
                    double upload = 0;

                    if (hasBaseline && !token.IsCancellationRequested)
                    {
                        download = Math.Max(0, received - lastReceived) / UpdateInterval.TotalSeconds;
                        upload = Math.Max(0, sent - lastSent) / UpdateInterval.TotalSeconds;
                    }

                    lastReceived = received;
                    lastSent = sent;
                    hasBaseline = !token.IsCancellationRequested;

                    if (token.IsCancellationRequested)
                        break;

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                        () => OnStatsUpdated(download, upload)
                    );
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void OnStatsUpdated(double download, double upload, bool clear = false)
        {
            if (clear)
            {
                DownloadPoints.Clear();
                UploadPoints.Clear();
            }
            else
            {
                DownloadPoints.Add(download);
                UploadPoints.Add(upload);

                if (DownloadPoints.Count > MaxPoints)
                    DownloadPoints.RemoveAt(0);

                if (UploadPoints.Count > MaxPoints)
                    UploadPoints.RemoveAt(0);
            }

            DownloadValue = download;
            UploadValue = upload;
            DownloadSpeed = FormatSpeed(download);
            UploadSpeed = FormatSpeed(upload);
        }

        private static (long received, long sent) GetInterfaceBytes()
        {
            long received = 0;
            long sent = 0;

            try
            {
                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    IPv4InterfaceStatistics statistics = networkInterface.GetIPv4Statistics();
                    received += statistics.BytesReceived;
                    sent += statistics.BytesSent;
                }
            }
            catch
            {
                return (received, sent);
            }

            return (received, sent);
        }

        private static string FormatSpeed(double bytesPerSecond)
        {
            string[] units = { "B/s", "KB/s", "MB/s", "GB/s" };
            int unit = 0;
            double value = Math.Max(0, bytesPerSecond);

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            string format = unit == 0 ? "0" : "0.0";
            return $"{value.ToString(format, CultureInfo.InvariantCulture)} {units[unit]}";
        }

        private static SKColor ResolveColor(string key)
        {
            if (System.Windows.Application.Current?.TryFindResource(key) is System.Windows.Media.Color color)
                return new SKColor(color.R, color.G, color.B);

            return SKColors.Transparent;
        }

        private static SKColor ResolveBrushColor(string key)
        {
            if (System.Windows.Application.Current?.TryFindResource(key) is System.Windows.Media.SolidColorBrush brush)
                return new SKColor(brush.Color.R, brush.Color.G, brush.Color.B);

            return SKColors.Transparent;
        }
    }
}
