using System.Windows;

namespace PasswordManager
{
    public partial class ImportPage : StandardUserControl
    {
        public ImportPage()
        {
            InitializeComponent();
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            util.importLastPassCSV(importTextBox.Text);

            // Refresh our hotkey match information to reflect the changes.
            ((MainWindow)App.Current.MainWindow).updateHotkeyMatchInfo();

            sp.updateTitle("advanced");
            sp.updateDisplayMessage("");
            sp.updateContent(SharedState.advancedPage);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            sp.updateTitle("advanced");
            sp.updateDisplayMessage("");
            sp.updateContent(SharedState.advancedPage);
        }
    }
}
