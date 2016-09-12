using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PasswordManager
{
    public partial class DetailPage : StandardUserControl
    {
        public WAppCpp.Site site { get; set; }

        private bool showTechDetails = false;

        private bool showPassword = false;

        public DetailPage()
        {
            InitializeComponent();
        }
        
        public void refreshPage(WAppCpp.Site _site)
        {
            site = WAppCpp.Util.Instance.getSite(_site);
            this.DataContext = site;
            showTechDetails = false;
            showPassword = false;
            passwordField.Text = "**********";
            showpwButton.Content = " show password ";
            deleteButton.IsEnabled = true;
            reallyDeleteButton.Visibility = Visibility.Hidden;
            dontDeleteButton.Visibility = Visibility.Hidden;

            // Update field visibility, to only show fields that aren't blank.
            urlPanel.Visibility = Visibility.Visible;
            userPanel.Visibility = Visibility.Visible;
            passwordPanel.Visibility = Visibility.Visible;
            notesPanel.Visibility = Visibility.Visible;
            if (string.IsNullOrWhiteSpace(site.url)) urlPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(site.user)) userPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(site.password)) passwordPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(site.notes)) notesPanel.Visibility = Visibility.Collapsed;
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            sp.updateTitle("edit site");
            sp.updateDisplayMessage("");
            SharedState.addEditSitePage.refreshPage(site);
            sp.updateContent(SharedState.addEditSitePage);
        }

        private void detailPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F10)
            {
                if (showTechDetails)
                {
                    showTechDetails = false;
                    fieldsPanel.Children.RemoveAt(0);
                    fieldsPanel.Children.RemoveAt(0);
                    fieldsPanel.Children.RemoveAt(0);
                }
                else
                {
                    showTechDetails = true;
                    TextBlock idBlock = new TextBlock();
                    idBlock.Text = "id = " + site.id;
                    idBlock.FontSize = 35;
                    idBlock.FontWeight = FontWeights.Bold;
                    fieldsPanel.Children.Insert(0, idBlock);
                    TextBlock sidBlock = new TextBlock();
                    sidBlock.Text = "site id = " + site.siteId;
                    sidBlock.FontSize = 35;
                    sidBlock.FontWeight = FontWeights.Bold;
                    fieldsPanel.Children.Insert(1, sidBlock);
                    TextBlock vBlock = new TextBlock();
                    vBlock.Text = "version = " + site.version;
                    vBlock.FontSize = 35;
                    vBlock.FontWeight = FontWeights.Bold;
                    fieldsPanel.Children.Insert(2, vBlock);
                }
            }
        }

        private void detailPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            deleteButton.IsEnabled = false;
            reallyDeleteButton.Visibility = Visibility.Visible;
            dontDeleteButton.Visibility = Visibility.Visible;
        }

        private void showpwButton_Click(object sender, RoutedEventArgs e)
        {
            if (showPassword)
            {
                showPassword = false;
                passwordField.Text = "**********";
                showpwButton.Content = " show password ";
            }
            else
            {
                showPassword = true;
                passwordField.Text = site.password;
                showpwButton.Content = " hide password ";
            }
        }

        private void reallyDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            WAppCpp.Util util = WAppCpp.Util.Instance;
            util.deleteSite(site.id);

            // Refresh our hotkey match information to reflect the changes.
            ((MainWindow)App.Current.MainWindow).updateHotkeyMatchInfo();

            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("search");
            standardPage.updateDisplayMessage("Site successfully deleted.");
            SharedState.searchPage.refreshPage();
            standardPage.updateContent(SharedState.searchPage);
        }

        private void dontDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            dontDeleteButton.Visibility = Visibility.Hidden;
            reallyDeleteButton.Visibility = Visibility.Hidden;
            deleteButton.IsEnabled = true;
        }

        private void copyUrlButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(site.url);
            sp.updateDisplayMessage("Url copied to clipboard.");
        }

        private void copyUserButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(site.user);
            sp.updateDisplayMessage("User copied to clipboard.");
        }

        private void copyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(site.password);
            sp.updateDisplayMessage("Password copied to clipboard.");
        }

        private void copyNotesButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(site.notes);
            sp.updateDisplayMessage("Notes copied to clipboard.");
        }

        public override bool childHandleKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = false;
            if (e.Key == Key.U)
            {
                copyUserButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.P)
            {
                copyPasswordButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.N)
            {
                copyNotesButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.A)
            {
                copyUrlButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.V)
            {
                showpwButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.E)
            {
                editButton_Click(null, null);
                handled = true;
            }
            else if (e.Key == Key.D)
            {
                deleteButton_Click(null, null);
                handled = true;
            }
            return handled;
        }

        private void launchUrlButton_Click(object sender, RoutedEventArgs e)
        {
            if (site.url.ToLower().StartsWith("http://") || site.url.ToLower().StartsWith("https://"))
            {
                System.Diagnostics.Process.Start(site.url);
            }
            else
            {
                System.Diagnostics.Process.Start("http://" + site.url);
            }
            sp.updateDisplayMessage("Site launched.");
        }
    }
}
