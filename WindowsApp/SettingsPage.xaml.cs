namespace PasswordManager
{
    public partial class SettingsPage : StandardUserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void settingsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            hotkeyCheckbox.IsChecked = SharedState.hotkeysEnabled;
            idleTimeoutCheckbox.IsChecked = SharedState.idleTimeoutEnabled;
            for (int i = 0; i < SharedState.idleTimeoutValues.Count; i++)
            {
                if (SharedState.idleTimeoutMillis == SharedState.idleTimeoutValues[i])
                {
                    idleTimeoutComboBox.SelectedIndex = i;
                    break;
                }
                else if (SharedState.idleTimeoutValues[i] > SharedState.idleTimeoutMillis)
                {
                    idleTimeoutComboBox.SelectedIndex = i == 0 ? 0 : i - 1;
                    break;
                }
            }
            idleTimeoutComboBox.IsEnabled = SharedState.idleTimeoutEnabled ? true : false;
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
        }

        private void hotkeyCheckbox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.hotkeysEnabled;
            SharedState.hotkeysEnabled = hotkeyCheckbox.IsChecked == true ? true : false;
            if (SharedState.hotkeysEnabled != prevSetting)
            {
                SharedState.writeSettingsFile();
                if (SharedState.hotkeysEnabled) ((MainWindow)App.Current.MainWindow).startHotkeyHandling();
                else ((MainWindow)App.Current.MainWindow).stopHotkeyHandling();
            }
        }

        private void idleTimeoutCheckbox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.idleTimeoutEnabled;
            SharedState.idleTimeoutEnabled = idleTimeoutCheckbox.IsChecked == true ? true : false;
            idleTimeoutComboBox.IsEnabled = SharedState.idleTimeoutEnabled ? true : false;
            if (SharedState.idleTimeoutEnabled != prevSetting) SharedState.writeSettingsFile();
        }

        private void idleTimeoutComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            long prevSetting = SharedState.idleTimeoutMillis;
            SharedState.idleTimeoutMillis = SharedState.idleTimeoutValues[idleTimeoutComboBox.SelectedIndex];
            if (SharedState.idleTimeoutMillis != prevSetting) SharedState.writeSettingsFile();
        }

        private void pwLength_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int prevSetting = SharedState.pwGenDefaultPwLength;
            SharedState.pwGenDefaultPwLength = SharedState.pwGenDefaultPwLengthValues[pwLength.SelectedIndex];
            if (SharedState.pwGenDefaultPwLength != prevSetting) SharedState.writeSettingsFile();
        }

        private void useNum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.pwGenDefaultUseNum;
            SharedState.pwGenDefaultUseNum = useNum.IsChecked == true ? true : false;
            if (SharedState.pwGenDefaultUseNum != prevSetting) SharedState.writeSettingsFile();
        }

        private void useAlphaLower_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.pwGenDefaultUseAlphaLower;
            SharedState.pwGenDefaultUseAlphaLower = useAlphaLower.IsChecked == true ? true : false;
            if (SharedState.pwGenDefaultUseAlphaLower != prevSetting) SharedState.writeSettingsFile();
        }

        private void useAlphaUpper_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.pwGenDefaultUseAlphaUpper;
            SharedState.pwGenDefaultUseAlphaUpper = useAlphaUpper.IsChecked == true ? true : false;
            if (SharedState.pwGenDefaultUseAlphaUpper != prevSetting) SharedState.writeSettingsFile();
        }

        private void useSymbols_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.pwGenDefaultUseSymbols;
            SharedState.pwGenDefaultUseSymbols = useSymbols.IsChecked == true ? true : false;
            if (SharedState.pwGenDefaultUseSymbols != prevSetting) SharedState.writeSettingsFile();
        }

        private void useAmbi_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool prevSetting = SharedState.pwGenDefaultUseAmbi;
            SharedState.pwGenDefaultUseAmbi = useAmbi.IsChecked == true ? true : false;
            if (SharedState.pwGenDefaultUseAmbi != prevSetting) SharedState.writeSettingsFile();
        }
    }
}
