using System.Windows;

namespace PasswordManager
{
    public partial class ChangeMasterPage : StandardUserControl
    {
        public ChangeMasterPage()
        {
            InitializeComponent();
        }

        private void changeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SharedState.areSecureStringsEqual(currentBox.SecurePassword, SharedState.password))
            {
                sp.updateDisplayMessage("Current password is incorrect.");
            }
            else if (!SharedState.areSecureStringsEqual(new1Box.SecurePassword, new2Box.SecurePassword))
            {
                sp.updateDisplayMessage("Passwords don't match.");
            }
            else
            {
                util.changeMasterPassword(new1Box.SecurePassword);
                SharedState.password = new1Box.SecurePassword;
                sp.updateTitle("search");
                sp.updateDisplayMessage("Master password successfully changed.");
                SharedState.searchPage.refreshPage();
                sp.updateContent(SharedState.searchPage);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            StandardPage standardPage = (App.Current.Resources["standardPage"] as StandardPage);
            standardPage.updateTitle("advanced");
            standardPage.updateDisplayMessage("");
            standardPage.updateContent(SharedState.advancedPage);
        }
    }
}
