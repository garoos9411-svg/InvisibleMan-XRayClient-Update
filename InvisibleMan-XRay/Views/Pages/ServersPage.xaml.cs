using System;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Views.Pages
{
    using ViewModels;

    public partial class ServersPage : Page
    {
        public event EventHandler OpenServerManagerRequested;

        public ServersPage()
        {
            InitializeComponent();
        }

        private void OnOpenServerManagerClick(object sender, RoutedEventArgs e)
        {
            OpenServerManagerRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnServerListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DataContext is ServersViewModel serversViewModel))
                return;

            if (ServersList.SelectedItem is ServerItemViewModel item)
            {
                serversViewModel.Select(item);
                ServersList.SelectedItem = null;
            }
        }
    }
}
