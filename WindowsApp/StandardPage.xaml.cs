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
    /// Interaction logic for StandardPage.xaml
    /// </summary>
    public partial class StandardPage : UserControl
    {
        public String title { get; set; }
        public String displayMessage { get; set; }
        public StandardUserControl content { get; set; }

        public bool leftCtrlDown = false;
        public bool rightCtrlDown = false;
        public bool leftShiftDown = false;
        public bool rightShiftDown = false;

        public StandardPage()
        {
            InitializeComponent();
            title = "default title";
            displayMessage = "";
            this.DataContext = this;
        }

        public void updateTitle(String title)
        {
            this.title = title;
            BindingOperations.GetBindingExpressionBase(pageTitle, TextBlock.TextProperty).UpdateTarget();
        }

        public void updateDisplayMessage(String displayMessage)
        {
            this.displayMessage = displayMessage;
            BindingOperations.GetBindingExpressionBase(pageDisplayMessage,
                TextBlock.TextProperty).UpdateTarget();
        }

        public void updateContent(StandardUserControl _content)
        {
            this.ContentRoot.Children.Clear();
            this.ContentRoot.Children.Add(_content);
            content = _content;
        }

        private void standardPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Any on loaded code goes here.
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            updateTitle("search");
            updateDisplayMessage("");
            SharedState.searchPage.refreshPage();
            updateContent(SharedState.searchPage);
        }

        private void listButton_Click(object sender, RoutedEventArgs e)
        {
            updateTitle("list");
            updateDisplayMessage("");
            updateContent(SharedState.listPage);
        }

        private void addSiteButton_Click(object sender, RoutedEventArgs e)
        {
            updateTitle("add site");
            updateDisplayMessage("");
            SharedState.addEditSitePage.refreshPage(null);
            updateContent(SharedState.addEditSitePage);
        }

        private void advancedButton_Click(object sender, RoutedEventArgs e)
        {
            updateTitle("advanced");
            updateDisplayMessage("");
            updateContent(SharedState.advancedPage);
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        public void StandardPage_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl) leftCtrlDown = false;
            else if (e.Key == Key.RightCtrl) rightCtrlDown = false;
            else if (e.Key == Key.LeftShift) leftShiftDown = false;
            else if (e.Key == Key.RightShift) rightShiftDown = false;
        }

        public async void StandardPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                leftCtrlDown = true;
                e.Handled = true;
            }
            else if (e.Key == Key.RightCtrl)
            {
                rightCtrlDown = true;
                e.Handled = true;
            }
            else if (e.Key == Key.LeftShift)
            {
                leftShiftDown = true;
                e.Handled = true;
            }
            else if (e.Key == Key.RightShift)
            {
                rightShiftDown = true;
                e.Handled = true;
            }
            else if (e.Key == Key.L && (rightCtrlDown || leftCtrlDown))
            {
                await Task.Delay(1);
                updateTitle("list");
                updateDisplayMessage("");
                updateContent(SharedState.listPage);
                e.Handled = true;
            }
            else if (e.Key == Key.S && (rightCtrlDown || leftCtrlDown))
            {
                await Task.Delay(1); // Allow this event to run its course (so not present in upcoming text field).
                updateTitle("search");
                updateDisplayMessage("");
                SharedState.searchPage.refreshPage();
                updateContent(SharedState.searchPage);
                e.Handled = true;
            }
            else if (e.Key == Key.A && (rightCtrlDown || leftCtrlDown))
            {
                await Task.Delay(1);
                updateTitle("add site");
                updateDisplayMessage("");
                SharedState.addEditSitePage.refreshPage(null);
                updateContent(SharedState.addEditSitePage);
                e.Handled = true;
            }
            //else if (e.Key == Key.X)
            //{
            //App.Current.Shutdown();
            //e.Handled = true;
            //}

            // If we didn't already handle it, let the child handle it.       
            if (!e.Handled)
            {
                e.Handled = content.childHandleKeyDown(sender, e);
            }
        }

    }
}
