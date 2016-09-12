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
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace PasswordManager
{
    /// <summary>
    /// Interaction logic for AddSitePage.xaml
    /// </summary>
    public partial class AddEditSitePage : StandardUserControl
    {
        private bool isNewSite = true;
        private WAppCpp.Site site { get; set; }

        public AddEditSitePage()
        {
            InitializeComponent();
            isNewSite = true;
        }

        public void refreshPage(WAppCpp.Site _site)
        {
            if (_site == null)
            {
                isNewSite = true;
                site = null;
                nameBox.Text = "";
                urlBox.Text = "";
                userBox.Text = "";
                passwordBox.Text = "";
                repeatPasswordBox.Text = "";
                notesBox.Text = "";
            }
            else
            {
                isNewSite = false;
                site = WAppCpp.Util.Instance.getSite(_site);
                nameBox.Text = site.name;
                urlBox.Text = site.url;
                userBox.Text = site.user;
                passwordBox.Text = site.password;
                repeatPasswordBox.Text = site.password;
                notesBox.Text = site.notes;
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);

            // Do data validation.
            if (nameBox.Text == null || nameBox.Text.Equals(""))
            {
                standardPage.updateDisplayMessage("Name must not be blank.");
                return;
            }
            if (!passwordBox.Text.Equals(repeatPasswordBox.Text))
            {
                standardPage.updateDisplayMessage("Passwords don't match");
                return;
            }

            // Process a new site.
            WAppCpp.Site refreshedSite;
            if (isNewSite)
            {
                WAppCpp.Util util = WAppCpp.Util.Instance;
                WAppCpp.Site site = new WAppCpp.Site();
                site.name = nameBox.Text;
                site.url = urlBox.Text;
                site.user = userBox.Text;
                site.password = passwordBox.Text;
                site.notes = notesBox.Text;
                site.version = 1;
                int newSiteId = util.saveSite(site);

                refreshedSite = util.getSite(newSiteId);
            }

            // Process an edited, existing site.
            else // isNewSite == false (we're editing an existing site)
            {
                WAppCpp.Util util = WAppCpp.Util.Instance;
                WAppCpp.Site editedSite = new WAppCpp.Site();
                editedSite.name = nameBox.Text;
                editedSite.url = urlBox.Text;
                editedSite.user = userBox.Text;
                editedSite.password = passwordBox.Text;
                editedSite.notes = notesBox.Text;
                editedSite.version = site.version + 1;
                editedSite.siteId = site.siteId;
                util.saveSite(editedSite);

                refreshedSite = util.getSite(site.siteId);
            }

            standardPage.updateTitle(refreshedSite.name);
            standardPage.updateDisplayMessage(isNewSite ? "Site successfully created." : "Site successfully updated.");
            SharedState.detailPage.refreshPage(refreshedSite);
            standardPage.updateContent(SharedState.detailPage);

            // Refresh our hotkey match information to reflect the changes.
            ((MainWindow)App.Current.MainWindow).updateHotkeyMatchInfo();
        }

        private void generatePwButton_Click(object sender, RoutedEventArgs e)
        {
            // Define password char classes.
            string pwNum = "0123456789";
            string pwNumNoAmbig = "123456789";
            string pwAlphaLower = "abcdefghijklmnopqrstuvwxyz";
            string pwAlphaLowerNoAmbig = "abcdefghijkmnopqrstuvwxyz";
            string pwAlphaUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string pwAlphaUpperNoAmbig = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string pwSymbols = "!@#$%^&*";  // "]!#$%&()*+,-./:;<=>?@[^_`{|}~";

            // Figure out which characters to choose from.
            string pwChars = "";
            if (useNum.IsChecked == true) 
                if (useAmbi.IsChecked == true) pwChars += pwNum; else pwChars += pwNumNoAmbig;
            if (useAlphaLower.IsChecked == true) 
                if (useAmbi.IsChecked == true) pwChars += pwAlphaLower; else pwChars += pwAlphaLowerNoAmbig;
            if (useAlphaUpper.IsChecked == true)
                if (useAmbi.IsChecked == true) pwChars += pwAlphaUpper; else pwChars += pwAlphaUpperNoAmbig;
            if (useSymbols.IsChecked == true) pwChars += pwSymbols;

            // If no symbol types are selected, don't do anything.
            if (pwChars.Length == 0) return;

            // Get desired password length.
            //ComboBoxItem selectedItem = (ComboBoxItem) pwLength.SelectedItem;
            //string selectedValue = selectedItem.Content.ToString();
            //int numPwChars;
            //int.TryParse(selectedValue, out numPwChars);
            int numPwChars = SharedState.pwGenDefaultPwLengthValues[pwLength.SelectedIndex];

            // Generate our password.
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            string pw = "";
            bool done = false;
            while (!done)
            {
                // Generate a candidate password.
                pw = "";
                for (int i = 0; i < numPwChars; i++)
                {
                    // Get a random floating point number between 0 and 1.
                    byte[] b = new byte[4];
                    rng.GetBytes(b);
                    UInt32 testVal = BitConverter.ToUInt32(b, 0);
                    double f = ((double)BitConverter.ToUInt32(b, 0)) / ((double)UInt32.MaxValue);

                    int nextIndex = (int)Math.Floor(f * (pwChars.Length + 1));
                    if (nextIndex == pwChars.Length) nextIndex--;
                    pw += pwChars[nextIndex];
                }

                // Check to see if the password meets criteria.
                bool meetsCriteria = true;
                if (numPwChars >= 4)
                {
                    if (useNum.IsChecked == true) if (!(new Regex(".*[0-9].*").IsMatch(pw))) meetsCriteria = false;
                    if (useAlphaLower.IsChecked == true) if (!(new Regex(".*[a-z].*").IsMatch(pw))) meetsCriteria = false;
                    if (useAlphaUpper.IsChecked == true) if (!(new Regex(".*[A-Z].*").IsMatch(pw))) meetsCriteria = false;
                    if (useSymbols.IsChecked == true) if (!(new Regex(".*[" + pwSymbols + "].*").IsMatch(pw))) meetsCriteria = false;
                }
                if (meetsCriteria) done = true;
            }

            // Populate the password fields.
            passwordBox.Text = pw;
            repeatPasswordBox.Text = pw;

            // Clear display message.
            sp.updateDisplayMessage("");
        }

        private void copypwButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(passwordBox.Text);
            sp.updateDisplayMessage("Password copied to the clipboard.");
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isNewSite)
            {
                sp.updateTitle("search");
                sp.updateDisplayMessage("");
                SharedState.searchPage.refreshPage();
                sp.updateContent(SharedState.searchPage);
            }
            else
            {
                sp.updateTitle(site.name);
                sp.updateDisplayMessage("");
                SharedState.detailPage.refreshPage(site);
                sp.updateContent(SharedState.detailPage);
            }
        }

        private void addEditSitePage_Loaded(object sender, RoutedEventArgs e)
        {
            useNum.IsChecked = SharedState.pwGenDefaultUseNum;
            useAlphaLower.IsChecked = SharedState.pwGenDefaultUseAlphaLower;
            useAlphaUpper.IsChecked = SharedState.pwGenDefaultUseAlphaUpper;
            useSymbols.IsChecked = SharedState.pwGenDefaultUseSymbols;
            useAmbi.IsChecked = SharedState.pwGenDefaultUseAmbi;
            bool pwLengthSet = false;
            for (int i = 0; i < SharedState.pwGenDefaultPwLengthValues.Count; i++)
            {
                if (SharedState.pwGenDefaultPwLength == SharedState.pwGenDefaultPwLengthValues[i])
                {
                    pwLength.SelectedIndex = i;
                    pwLengthSet = true;
                    break;
                }
            }
            if (!pwLengthSet) pwLength.SelectedIndex = 0;
            updateGenPwButtonEnabled(sender, e);
            nameBox.Focus();
        }

        private void updateGenPwButtonEnabled(object sender, RoutedEventArgs e)
        {
            if (!(useAlphaLower.IsChecked == true) && !(useAlphaUpper.IsChecked == true)
                && !(useSymbols.IsChecked == true) && !(useNum.IsChecked == true))
            {
                generatePwButton.IsEnabled = false;
            }
            else
            {
                generatePwButton.IsEnabled = true;
            }
        }
    }
}
