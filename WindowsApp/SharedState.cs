using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace PasswordManager
{
    public static class SharedState
    {
        // Shared variables (general).
        public static ObservableCollection<WAppCpp.Site> sites { get; set; }
        public static ObservableCollection<WAppCpp.Site> searchResults { get; set; }
        public static ObservableCollection<Vault> vaults { get; set; }
        public static int selectedVaultIndex;
        public static SecureString password { get; set; }
        public static Dispatcher uiDispatcher { get; set; }

        // Shared settings variables.
        public static bool hotkeysEnabled { get; set; }
        public static bool idleTimeoutEnabled { get; set; }
        public static long idleTimeoutMillis { get; set; }
        public static ObservableCollection<string> idleTimeoutChoices { get; set; }
        public static ObservableCollection<long> idleTimeoutValues { get; set; }
        public static bool pwGenDefaultUseNum { get; set; }
        public static bool pwGenDefaultUseAlphaLower { get; set; }
        public static bool pwGenDefaultUseAlphaUpper { get; set; }
        public static bool pwGenDefaultUseSymbols { get; set; }
        public static bool pwGenDefaultUseAmbi { get; set; }
        public static int pwGenDefaultPwLength { get; set; }
        public static ObservableCollection<string> pwGenDefaultPwLengthChoices { get; set; }
        public static ObservableCollection<int> pwGenDefaultPwLengthValues { get; set; }


        // Shared page content instances.
        public static WelcomePage welcomePage { get; set; }
        public static StandardPage standardPage { get; set; }
        public static SearchPage searchPage { get; set; }
        public static ListPage listPage { get; set; }
        public static DetailPage detailPage { get; set; }
        public static AddEditSitePage addEditSitePage { get; set; }
        public static AdvancedPage advancedPage { get; set; }
        public static ImportPage importPage { get; set; }
        public static ExportPage exportPage { get; set; }
        public static ChangeMasterPage changeMasterPage { get; set; }
        public static BluetoothUpdatePage bluetoothUpdatePage { get; set; }
        public static SettingsPage settingsPage { get; set; }

        static SharedState()
        {
            // Initialize general shared variables.
            sites = new ObservableCollection<WAppCpp.Site>();
            searchResults = new ObservableCollection<WAppCpp.Site>();
            vaults = new ObservableCollection<Vault>();
            selectedVaultIndex = 0;
            uiDispatcher = App.Current.Dispatcher;

            // Initialize settings variables.
            hotkeysEnabled = true;
            idleTimeoutEnabled = true;
            idleTimeoutMillis = 20 * 60 * 1000; // Twenty minutes.
            idleTimeoutChoices = new ObservableCollection<string>();
            idleTimeoutValues = new ObservableCollection<long>();
            idleTimeoutChoices.Add("1 minute");
            idleTimeoutValues.Add(1 * 60 * 1000);
            idleTimeoutChoices.Add("2 minutes");
            idleTimeoutValues.Add(2 * 60 * 1000);
            idleTimeoutChoices.Add("3 minutes");
            idleTimeoutValues.Add(3 * 60 * 1000);
            idleTimeoutChoices.Add("5 minutes");
            idleTimeoutValues.Add(5 * 60 * 1000);
            idleTimeoutChoices.Add("10 minutes");
            idleTimeoutValues.Add(10 * 60 * 1000);
            idleTimeoutChoices.Add("15 minutes");
            idleTimeoutValues.Add(15 * 60 * 1000);
            idleTimeoutChoices.Add("20 minutes");
            idleTimeoutValues.Add(20 * 60 * 1000);
            idleTimeoutChoices.Add("30 minutes");
            idleTimeoutValues.Add(30 * 60 * 1000);
            idleTimeoutChoices.Add("1 hour");
            idleTimeoutValues.Add(1 * 60 * 60 * 1000);
            idleTimeoutChoices.Add("2 hours");
            idleTimeoutValues.Add(2 * 60 * 60 * 1000);
            idleTimeoutChoices.Add("3 hours");
            idleTimeoutValues.Add(3 * 60 * 60 * 1000);
            idleTimeoutChoices.Add("5 hours");
            idleTimeoutValues.Add(5 * 60 * 60 * 1000);
            idleTimeoutChoices.Add("10 hours");
            idleTimeoutValues.Add(10 * 60 * 60 * 1000);
            idleTimeoutChoices.Add("1 day");
            idleTimeoutValues.Add(1 * 24 * 60 * 60 * 1000);
            pwGenDefaultUseNum = true;
            pwGenDefaultUseAlphaLower = true;
            pwGenDefaultUseAlphaUpper = true;
            pwGenDefaultUseSymbols = true;
            pwGenDefaultUseAmbi = false;
            pwGenDefaultPwLength = 16;
            pwGenDefaultPwLengthChoices = new ObservableCollection<string>();
            pwGenDefaultPwLengthValues = new ObservableCollection<int>();
            for (int i = 1; i < 31; i++)
            {
                pwGenDefaultPwLengthChoices.Add("" + i);
                pwGenDefaultPwLengthValues.Add(i);
            }

            // Initialize page instances.
            welcomePage = new WelcomePage();
            standardPage = new StandardPage();
            searchPage = new SearchPage();
            listPage = new ListPage();
            detailPage = new DetailPage();
            addEditSitePage = new AddEditSitePage();
            advancedPage = new AdvancedPage();
            importPage = new ImportPage();
            exportPage = new ExportPage();
            changeMasterPage = new ChangeMasterPage();
            bluetoothUpdatePage = new BluetoothUpdatePage();
            settingsPage = new SettingsPage();
        }


        public static void loadSettingsFile()
        {
            // Load settings file.
            XElement doc = XElement.Load(Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings.xml"));

            // Load whether hotkeys are enabled.
            SharedState.hotkeysEnabled = readXmlBool(doc, "enable-hotkeys", SharedState.hotkeysEnabled);

            // Load idle timeout.
            SharedState.idleTimeoutEnabled = readXmlBool(doc, "enable-idle-timeout", SharedState.idleTimeoutEnabled);
            SharedState.idleTimeoutMillis = readXmlLong(doc, "idle-timeout-millis", SharedState.idleTimeoutMillis);

            // Load password generation defaults.
            XElement pwGenDefaultElement = doc.Element("password-generation-defaults");
            SharedState.pwGenDefaultUseNum = readXmlBool(pwGenDefaultElement, "use-num", SharedState.pwGenDefaultUseNum);
            SharedState.pwGenDefaultUseAlphaLower = readXmlBool(pwGenDefaultElement, "use-alpha-lower", SharedState.pwGenDefaultUseAlphaLower);
            SharedState.pwGenDefaultUseAlphaUpper = readXmlBool(pwGenDefaultElement, "use-alpha-upper", SharedState.pwGenDefaultUseAlphaUpper);
            SharedState.pwGenDefaultUseSymbols = readXmlBool(pwGenDefaultElement, "use-symbols", SharedState.pwGenDefaultUseSymbols);
            SharedState.pwGenDefaultUseAmbi = readXmlBool(pwGenDefaultElement, "use-ambi", SharedState.pwGenDefaultUseAmbi);
            SharedState.pwGenDefaultPwLength = readXmlInt(pwGenDefaultElement, "pw-length", SharedState.pwGenDefaultPwLength);

            // Load info about known vaults.
            SharedState.vaults.Clear();
            IEnumerable<XElement> vaultsList = doc.Element("vaults").Elements("vault");
            foreach (XElement vaultElement in vaultsList)
            {
                Vault thisVault = new Vault();
                thisVault.location = vaultElement.Element("location").Value;
                thisVault.friendlyName = vaultElement.Element("friendly-name").Value;
                XElement isStartupVaultElement = vaultElement.Element("is-startup-vault");
                string isStartupVaultString = isStartupVaultElement == null ? "" : isStartupVaultElement.Value.ToLower();
                bool isStartupVault = false;
                Boolean.TryParse(isStartupVaultString, out isStartupVault);
                thisVault.isStartupVault = isStartupVault;
                thisVault.isTempVault = false;
                SharedState.vaults.Add(thisVault);
            }

            // Set selected vault index.
            bool selectedVaultSet = false;
            for (int i = 0; i < SharedState.vaults.Count; i++)
            {
                if (SharedState.vaults[i].isStartupVault)
                {
                    SharedState.selectedVaultIndex = i;
                    selectedVaultSet = true;
                    break;
                }
            }
            if (SharedState.vaults.Count > 0 && selectedVaultSet == false)
            {
                SharedState.selectedVaultIndex = 0;
                selectedVaultSet = true;
            }
        }


        private static bool readXmlBool(XElement parent, string elementName, bool defaultValue)
        {
            if (parent == null) return defaultValue;
            XElement element = parent.Element(elementName);
            if (element == null || String.IsNullOrWhiteSpace(element.Value)) return defaultValue;
            bool tempValue = defaultValue;
            bool.TryParse(element.Value, out tempValue);
            return tempValue;
        }


        private static long readXmlLong(XElement parent, string elementName, long defaultValue)
        {
            if (parent == null) return defaultValue;
            XElement element = parent.Element(elementName);
            if (element == null || String.IsNullOrWhiteSpace(element.Value)) return defaultValue;
            long tempValue = defaultValue;
            long.TryParse(element.Value, out tempValue);
            return tempValue;
        }


        private static int readXmlInt(XElement parent, string elementName, int defaultValue)
        {
            if (parent == null) return defaultValue;
            XElement element = parent.Element(elementName);
            if (element == null || String.IsNullOrWhiteSpace(element.Value)) return defaultValue;
            int tempValue = defaultValue;
            int.TryParse(element.Value, out tempValue);
            return tempValue;
        }


        public static void writeSettingsFile()
        {
            /*
            vaultsToSave.Sort((a, b) =>
            {
                int result = String.Compare(a.friendlyName, b.friendlyName);
                if (result == 0) result = String.Compare(a.location, b.location);
                return result;
            });*/

            // Create a StringBuilder for creating the settings file string.
            StringBuilder sb = new StringBuilder();

            // XML preamble and root element.
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n");
            sb.Append("<pm>\r\n");

            // Enable hotkeys.
            sb.Append("<enable-hotkeys>" + (hotkeysEnabled ? "true" : "false") + "</enable-hotkeys>\r\n");

            // Idle timeout.
            sb.Append("<enable-idle-timeout>" + (idleTimeoutEnabled ? "true" : "false") + "</enable-idle-timeout>\r\n");
            sb.Append("<idle-timeout-millis>" + idleTimeoutMillis + "</idle-timeout-millis>\r\n");

            // Password generation defaults.
            sb.Append("<password-generation-defaults>\r\n");
            sb.Append("<use-num>" + (pwGenDefaultUseNum ? "true" : "false") + "</use-num>\r\n");
            sb.Append("<use-alpha-lower>" + (pwGenDefaultUseAlphaLower ? "true" : "false") + "</use-alpha-lower>\r\n");
            sb.Append("<use-alpha-upper>" + (pwGenDefaultUseAlphaUpper ? "true" : "false") + "</use-alpha-upper>\r\n");
            sb.Append("<use-symbols>" + (pwGenDefaultUseSymbols ? "true" : "false") + "</use-symbols>\r\n");
            sb.Append("<use-ambi>" + (pwGenDefaultUseAmbi ? "true" : "false") + "</use-ambi>\r\n");
            sb.Append("<pw-length>" + pwGenDefaultPwLength + "</pw-length>\r\n");
            sb.Append("</password-generation-defaults>\r\n");

            // Vaults.
            sb.Append("<vaults>\r\n");
            foreach (Vault vault in SharedState.vaults)
            {
                sb.Append("<vault>\r\n");
                sb.Append("<location>" + vault.location + "</location>\r\n");
                sb.Append("<friendly-name>" + vault.friendlyName + "</friendly-name>\r\n");
                sb.Append("<is-startup-vault>" + vault.isStartupVault.ToString() + "</is-startup-vault>\r\n");
                sb.Append("</vault>\r\n");
            }
            sb.Append("</vaults>\r\n");

            // End root element.
            sb.Append("</pm>");

            // Write the file.
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetEntryAssembly().Location), "Settings.xml"), sb.ToString());
        }


        public static bool areSecureStringsEqual(SecureString str1, SecureString str2)
        {
	        // Check for null strings.
            if (str1 == null && str2 == null) return true;
            else if (str1 == null || str2 == null) return false;

            // Check for different lengths.
            if (str1.Length != str2.Length) return false;
            
            IntPtr s1Ptr = IntPtr.Zero;
            IntPtr s2Ptr = IntPtr.Zero;
	        try
	        {
                s1Ptr = Marshal.SecureStringToCoTaskMemUnicode(str1);
                s2Ptr = Marshal.SecureStringToCoTaskMemUnicode(str2);
                bool done = false;
                for (int i = 0, j = 0; !done; i+=2, j+=2)
                {
                    // UTF-16 are always 2 or 4 bytes, and terminator is double 0, so this works.
                    if (Marshal.ReadInt16(s1Ptr, i) != Marshal.ReadInt16(s2Ptr, j)) return false;
                    if (Marshal.ReadInt16(s1Ptr, i) == 0 || Marshal.ReadInt16(s2Ptr, j) == 0) done = true;
                }
	        }
	        finally
	        {
                // Erase plaintext.
                Marshal.ZeroFreeCoTaskMemUnicode(s1Ptr);
                Marshal.ZeroFreeCoTaskMemUnicode(s2Ptr);
	        }

            return true;
        }


        public static T FindChild<T>(DependencyObject depObj, string childName)
        where T : DependencyObject
        {
            // Confirm obj is valid. 
            if (depObj == null) return null;

            // success case
            if (depObj is T && ((FrameworkElement)depObj).Name == childName)
                return depObj as T;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                //DFS
                T obj = FindChild<T>(child, childName);

                if (obj != null)
                    return obj;
            }

            return null;
        }

    }
}


