using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PasswordManager
{
    /// <summary>
    /// Interaction logic for AdvancedPage.xaml
    /// </summary>
    public partial class AdvancedPage : StandardUserControl
    {
        public AdvancedPage()
        {
            InitializeComponent();
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("import");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.importPage);
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("export");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.exportPage);
        }

        private void changePwButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("change password");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.changeMasterPage);
        }

        private void bluetoothUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("bluetooth update");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.bluetoothUpdatePage);
        }

        public override bool childHandleKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = false;
            if (e.Key == Key.I)
            {
                importButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.E)
            {
                exportButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.C)
            {
                changePwButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.B)
            {
                bluetoothUpdateButton_Click(null, null);
                handled = true;
            }
            return handled;
        }

        private void changeVaultButton_Click(object sender, RoutedEventArgs e)
        {
            Switcher.Switch(new WelcomePage());
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("settings");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.settingsPage);
        }
    }
}
