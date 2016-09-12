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
    public partial class SearchPage : StandardUserControl
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        // Refresh / reset this page.
        public void refreshPage()
        {
            searchResults.Template = (ControlTemplate)Resources["before_search"];
            SharedState.searchResults.Clear();
            searchBox.Text = "";
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                String searchString = searchBox.Text;
                WAppCpp.Util util = WAppCpp.Util.Instance;
                List<WAppCpp.Site> searchResultsList = util.search(searchString);
                if (searchResultsList.Count() == 0)
                {
                    searchResults.Template = (ControlTemplate) Resources["no_results"];
                }
                else if (searchResultsList.Count() == 1)
                {
                    sp.updateTitle(searchResultsList[0].name);
                    sp.updateDisplayMessage("");
                    SharedState.detailPage.refreshPage(searchResultsList[0]);
                    sp.updateContent(SharedState.detailPage);
                }
                else
                {
                    searchResults.Template = (ControlTemplate)Resources["results"];
                    SharedState.searchResults.Clear();
                    foreach (WAppCpp.Site thisSite in searchResultsList)
                    {
                        App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        { SharedState.searchResults.Add(thisSite); }));
                    }
                }
            }
        }

        private void searchListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is ListViewItem)) dep = VisualTreeHelper.GetParent(dep);
            if (dep == null) return;

            ListView searchListView = (ListView)sender;
            WAppCpp.Site thisSite =
                (WAppCpp.Site)searchListView.ItemContainerGenerator.ItemFromContainer(dep);

            sp.updateTitle(thisSite.name);
            sp.updateDisplayMessage("");
            SharedState.detailPage.refreshPage(thisSite);
            sp.updateContent(SharedState.detailPage);
        }

        private void searchPage_Loaded(object sender, RoutedEventArgs e)
        {
            SharedState.searchResults.Clear();
            searchBox.Focus();
        }
    }
}
