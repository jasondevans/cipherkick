using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

namespace PasswordManager
{
    public partial class WelcomePage : UserControl
    {
        StandardPage standardPage;

        public String displayMessage { get; set; }
        
        public WelcomePage()
        {
            InitializeComponent();
            standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            displayMessage = "";
            this.DataContext = this;
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                handlePasswordSubmit();
            }
        }

        private void handlePasswordSubmit()
        {
            // Check to see if the file exists.  If not, display an error.
            if (!File.Exists(SharedState.vaults[SharedState.selectedVaultIndex].location))
            {
                updateDisplayMessage("No vault file exists at this location.");
                passwordBox.Clear();
                return;
            }

            WAppCpp.Util util = WAppCpp.Util.Instance;
            try
            {
                SharedState.password = passwordBox.SecurePassword;
                Vault currentVault = SharedState.vaults[SharedState.selectedVaultIndex];
                util.openDbFile(currentVault.location, passwordBox.SecurePassword);
            }
            catch (WAppCpp.UtilException)
            {
                updateDisplayMessage("Password incorrect (or other problem opening the vault).");
                passwordBox.Clear();
                return;
            }

            // Do post-db-open processing.
            doPostDbOpenProcessing();
        }

        private void doPostDbOpenProcessing()
        {
            // Get a reference to our util singleton.
            WAppCpp.Util util = WAppCpp.Util.Instance;

            // Get metadata for this vault (to get friendly name).
            WAppCpp.Metainfo metadata = util.getMetadata();

            // Get a reference to the selected vault.
            Vault selectedVault = SharedState.vaults[SharedState.selectedVaultIndex];

            // If this was a new (to us) vault, or if the friendly name in the database
            // doesn't match our friendly name, or if this vault is not the startup vault,
            // update our settings file.
            if (selectedVault.isTempVault == true ||
                !selectedVault.friendlyName.Equals(metadata.friendlyName) ||
                selectedVault.isStartupVault == false)
            {
                // Update friendly name.
                SharedState.vaults[SharedState.selectedVaultIndex].friendlyName = metadata.friendlyName;

                // Make the selected vault no longer a temp vault.
                SharedState.vaults[SharedState.selectedVaultIndex].isTempVault = false;

                // Make the selected vault the startup vault.
                //SharedState.vaults[SharedState.selectedVaultIndex].isStartupVault = true;
                for (int i = 0; i < SharedState.vaults.Count; i++)
                {
                    SharedState.vaults[i].isStartupVault = (i == SharedState.selectedVaultIndex) ? true : false;
                }

                // Write the settings file.
                SharedState.writeSettingsFile();
            }

            // Do global initialization.
            ((MainWindow)App.Current.MainWindow).postPasswordInitialize();

            // Load the search page.
            standardPage.updateTitle("search");
            standardPage.updateDisplayMessage("");
            SharedState.searchPage.refreshPage();
            standardPage.updateContent(SharedState.searchPage);
            Switcher.Switch(standardPage);
        }

        private void welcomePage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load settings file.
                SharedState.loadSettingsFile();
            }
            catch (System.IO.FileNotFoundException)
            {
                // Write a default settings file.
                SharedState.writeSettingsFile();
            }

            // Set combo box selection.
            if (SharedState.vaults.Count > 0)
            {
                vaultsComboBox.SelectedIndex = SharedState.selectedVaultIndex;
            }

            // Update display depending on how many vaults we have.
            ObservableCollection<Vault> vaultList = SharedState.vaults;
            if (vaultList.Count == 0)
            {
                passwordBox.IsEnabled = false;
                goButton.IsEnabled = false;
                updateDisplayMessage("No vaults yet.  Click \"new...\" or \"open...\" to get started.");
                vaultsComboBox.Visibility = Visibility.Collapsed;
            }
            else // if (vaultList.Count > 0)
            {
                passwordBox.IsEnabled = true;
                goButton.IsEnabled = true;
                updateDisplayMessage("Enter vault password, or select a different vault.");
            }

            // Set focus in the password box.
            if (vaultList.Count > 0)
            {
                passwordBox.Clear();
                passwordBox.Focus();
            }
        }

        private void newButton_Click(object sender, RoutedEventArgs e)
        {
            newButton.IsEnabled = false;
            createVaultPanel.Visibility = Visibility.Visible;
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            Nullable<bool> result = ofd.ShowDialog(App.Current.MainWindow);
            if (result == true)
            {
                // Check to see if we already have this vault in our list.
                bool alreadyHaveThisOne = false;
                int indexToSelect = 0;
                for (int i = 0; i < SharedState.vaults.Count; i++)
                {
                    if (String.Equals(SharedState.vaults[i].location, ofd.FileName))
                    {
                        alreadyHaveThisOne = true;
                        indexToSelect = i;
                        break;
                    }
                }

                // If we don't have this vault already, add it to our list.
                if (alreadyHaveThisOne == false)
                {
                    Vault existingVault = new Vault();
                    existingVault.friendlyName = "[Vault to open]";
                    existingVault.location = ofd.FileName;
                    existingVault.isStartupVault = false;
                    existingVault.isTempVault = true;
                    addAndSelectVault(existingVault);
                }
                // Otherwise, just update our selection.
                else
                {
                    updateSelectedVault(indexToSelect);
                }

                // Attempt to set focus on the password box.
                passwordBox.Clear();
                passwordBox.Focus();
            }
        }

        private void updateSelectedVault(int indexToSelect)
        {
            // Update selected vault.
            SharedState.selectedVaultIndex = indexToSelect;
            //SharedState.selectedVault.Insert(0, SharedState.vaults[indexToSelect]);
            //if (SharedState.selectedVault.Count > 1) SharedState.selectedVault.RemoveAt(1);
            vaultsComboBox.SelectedIndex = indexToSelect;
        }

        private void addAndSelectVault(Vault vault)
        {
            // Remove any existing temporary vaults.
            for (int i = 0; i < SharedState.vaults.Count; i++)
            {
                if (SharedState.vaults[i].isTempVault)
                {
                    SharedState.vaults.RemoveAt(i);
                    i--;
                }
            }

            // Add vault to our vault list.
            SharedState.vaults.Add(vault);

            // Get new selected index.
            int indexToSelect = SharedState.vaults.Count - 1;

            // Update selected vault.
            updateSelectedVault(indexToSelect);

            // Make sure everything is set up properly for having at least one vault.
            passwordBox.IsEnabled = true;
            goButton.IsEnabled = true;
            vaultsComboBox.Visibility = Visibility.Visible;
            updateDisplayMessage("Enter vault password, or select a different vault.");
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            handlePasswordSubmit();
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            // Do some data validation.
            if (String.IsNullOrWhiteSpace(vaultNameBox.Text))
            {
                updateDisplayMessage("You must enter a vault name.");
                return;
            }
            if (!String.Equals(newpw1Box.Text, newpw2Box.Text))
            {
                updateDisplayMessage("Passwords do not match.");
                return;
            }
            if (String.IsNullOrWhiteSpace(newpw1Box.Text))
            {
                updateDisplayMessage("You must enter a vault password.");
                return;
            }
            if (!String.IsNullOrWhiteSpace(vaultLocBox.Text) && File.Exists(vaultLocBox.Text))
            {
                updateDisplayMessage("File already exists -- you must use a new file name.");
                return;
            }

            // Get the file location where to create the vault.
            String newVaultPath = "";
            if (!String.IsNullOrWhiteSpace(vaultLocBox.Text))
            {
                // Try to create the specified directory (if one is specified). If there are
                // any exceptions, the directory isn't valid, or there was an error creating
                // it.  This also works if the directory exists.
                try
                {
                    Directory.CreateDirectory(new FileInfo(vaultLocBox.Text.Trim()).Directory.FullName);
                }
                catch (Exception)
                {
                    updateDisplayMessage("Invalid filename, or unable to create directory.");
                    return;
                }
                newVaultPath = vaultLocBox.Text.Trim();
            }
            else
            {
                // No file was specified.  Create a file in the same directory as the executable,
                // which is the next unused file in the sequence Pm1.db, Pm2.db, etc.
                try
                {
                    String dirStr = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string[] fileStrs = Directory.GetFiles(dirStr, "Pm*.db");
                    List<int> indexes = new List<int>();
                    Regex reg = new Regex(@"Pm(?<idx>[0-9]+)\.db");
                    foreach (string thisFile in fileStrs)
                    {
                        MatchCollection mc = reg.Matches(thisFile);
                        String indexStr = null;
                        if (mc.Count > 0) indexStr = mc[0].Groups["idx"].Value;
                        int index = -1;
                        int.TryParse(indexStr, out index);
                        if (index != -1 && index != 0) indexes.Add(index);
                    }
                    indexes.Sort();
                    int nextIndex = 0;
                    if (indexes.Count == 0)
                    {
                        nextIndex = 1;
                    }
                    else if (indexes[0] > 1)
                    {
                        nextIndex = 1;
                    }
                    else // (indexes.Count > 0 && indexes[0] == 1)
                    {
                        int firstIndex = indexes[0];
                        for (int i = 0; i < indexes.Count; i++)
                        {
                            if (indexes[i] > (firstIndex + i))
                            {
                                nextIndex = firstIndex + i;
                                break;
                            }
                        }
                        if (nextIndex == 0) nextIndex = indexes[indexes.Count - 1] + 1;
                    }
                    newVaultPath = Path.Combine(dirStr, "Pm" + nextIndex + ".db");
                }
                catch (Exception)
                {
                    updateDisplayMessage("Error creating file.");
                    return;
                }
            }
            
            // Get a reference to our util singleton.
            WAppCpp.Util util = WAppCpp.Util.Instance;

            // Attempt to create the database.
            try
            {
                SharedState.password = new SecureString();
                foreach (char c in newpw1Box.Text) SharedState.password.AppendChar(c);
                util.openDbFile(newVaultPath, SharedState.password);
                util.setupNewDb(vaultNameBox.Text.Trim());
            }
            catch (WAppCpp.UtilException uex)
            {
                updateDisplayMessage("Error: " + uex.Message);
                return;
            }

            // Create the new vault.
            Vault newVault = new Vault();
            newVault.friendlyName = vaultNameBox.Text.Trim();
            newVault.location = newVaultPath;
            newVault.isStartupVault = true;
            newVault.isTempVault = true;
            addAndSelectVault(newVault);

            // Do post-db-open processing (including switching to the next screen).
            doPostDbOpenProcessing();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            createVaultPanel.Visibility = Visibility.Hidden;
            newButton.IsEnabled = true;
        }

        private void locButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "Pm";
            sfd.DefaultExt = ".db";
            sfd.Filter = "Password Manager database|*.db";
            Nullable<bool> result = sfd.ShowDialog(App.Current.MainWindow);
            if (result == true)
            {
                vaultLocBox.Text = sfd.FileName;
            }
        }

        private void vaultsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update selected vault, if there is a real selection.
            if (vaultsComboBox.SelectedIndex >= 0)
            {
                // Save the selected index.
                SharedState.selectedVaultIndex = vaultsComboBox.SelectedIndex;

                // Remove any temp vaults that are not selected.
                for (int i = 0; i < SharedState.vaults.Count; i++)
                {
                    if (SharedState.vaults[i].isTempVault && i != SharedState.selectedVaultIndex)
                    {
                        SharedState.vaults.RemoveAt(i);
                        if (i < SharedState.selectedVaultIndex) SharedState.selectedVaultIndex--;
                        i--;
                    }
                }
                if (vaultsComboBox.SelectedIndex != SharedState.selectedVaultIndex)
                {
                    vaultsComboBox.SelectedIndex = SharedState.selectedVaultIndex;
                }

                //Vault thisVault = (Vault)vaultsComboBox.SelectedItem;
                //SharedState.selectedVault.Insert(0, thisVault);
                //SharedState.selectedVault.RemoveAt(1);
            }
        }

        public void updateDisplayMessage(String displayMessage)
        {
            this.displayMessage = displayMessage;
            BindingOperations.GetBindingExpressionBase(pageDisplayMessage,
                TextBlock.TextProperty).UpdateTarget();
        }

    }
}
