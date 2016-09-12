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
    public partial class ListPage : StandardUserControl
    {
        public ListPage()
        {
            InitializeComponent();
        }

        private int selectedIndex = 0;

        private void siteListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is ListViewItem)) dep = VisualTreeHelper.GetParent(dep);
            if (dep == null) return;

            WAppCpp.Site thisSite = 
                (WAppCpp.Site)siteListView.ItemContainerGenerator.ItemFromContainer(dep);

            sp.updateTitle(thisSite.name);
            sp.updateDisplayMessage("");
            SharedState.detailPage.refreshPage(thisSite);
            sp.updateContent(SharedState.detailPage);
        }

        public override bool childHandleKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = false;
            if (e.Key == Key.J || e.Key == Key.Down)
            {
                selectedIndex = Math.Min(selectedIndex + 1, SharedState.sites.Count() - 1);
                siteListView.SelectedIndex = selectedIndex;
                siteListView.ScrollIntoView(siteListView.Items[selectedIndex]);
                handled = true;
            }
            else if (e.Key == Key.K || e.Key == Key.Up)
            {
                selectedIndex = Math.Max(selectedIndex - 1, 0);
                siteListView.SelectedIndex = selectedIndex;
                siteListView.ScrollIntoView(siteListView.Items[selectedIndex]);
                handled = true;
            }
            else if ((e.Key == Key.G && (sp.leftShiftDown || sp.rightShiftDown)) || e.Key == Key.End)
            {
                selectedIndex = Math.Max(0, SharedState.sites.Count() - 1);
                siteListView.SelectedIndex = selectedIndex;
                siteListView.ScrollIntoView(siteListView.Items[selectedIndex]);
                handled = true;
            }
            else if (e.Key == Key.Home)
            {
                selectedIndex = 0;
                siteListView.SelectedIndex = selectedIndex;
                siteListView.ScrollIntoView(siteListView.Items[selectedIndex]);
                handled = true;
            }
            else if (e.Key == Key.Left || e.Key == Key.Right)
            {
                // Don't do anything (i.e., don't change focus to a list element we don't want).
                handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                sp.updateTitle(SharedState.sites[selectedIndex].name);
                sp.updateDisplayMessage("");
                SharedState.detailPage.refreshPage(SharedState.sites[selectedIndex]);
                sp.updateContent(SharedState.detailPage);
                handled = true;
            }
            return handled;
        }

        private async void listPage_Loaded(object sender, RoutedEventArgs e)
        {
            WAppCpp.Util util = WAppCpp.Util.Instance;
            List<WAppCpp.Site> siteList = util.getSiteList();
            SharedState.sites.Clear();
            foreach (WAppCpp.Site thisSite in siteList)
            {
                await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                { SharedState.sites.Add(thisSite); }));
            }
            if (siteList.Count > 0)
            {
                siteListView.Visibility = Visibility.Visible;
                noSitesText.Visibility = Visibility.Collapsed;
                siteListView.Focus();
                await Task.Delay(1); // Give the site list view a chance to populate.
                siteListView.SelectedIndex = selectedIndex;
                siteListView.ScrollIntoView(siteListView.Items[selectedIndex]);
            }
            else // siteList.Count == 0
            {
                siteListView.Visibility = Visibility.Collapsed;
                noSitesText.Visibility = Visibility.Visible;
            }
        }
    }
}
