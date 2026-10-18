using System;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Views.Pages
{
    public partial class ProfilePage : Page
    {
        public event EventHandler ManageServersRequested;

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            ManageServersRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
