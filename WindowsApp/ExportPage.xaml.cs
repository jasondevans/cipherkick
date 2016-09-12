using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PasswordManager
{
    public partial class ExportPage : StandardUserControl
    {
        public ExportPage()
        {
            InitializeComponent();
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("name,url,user,password,notes\r\n");

            List<WAppCpp.Site> sites = util.getSiteList();
            foreach(WAppCpp.Site site in sites)
            {
                WAppCpp.Site fullSite = util.getSite(site);
                sb.Append(csvEscape(fullSite.name) + ",");
                sb.Append(csvEscape(fullSite.url) + ",");
                sb.Append(csvEscape(fullSite.user) + ",");
                sb.Append(csvEscape(fullSite.password) + ",");
                sb.Append(csvEscape(fullSite.notes) + "\r\n");
            }

            exportDataTextBox.Text = sb.ToString();
        }

        private string csvEscape(string input)
        {
            bool needsSurroundingQuotes = 
                (input.Contains("\"") || input.Contains(",") || input.Contains("\n")) ? true : false;
            input = input.Replace("\"", "\"\"");
            if (needsSurroundingQuotes)
            {
                return "\"" + input + "\"";
            }
            else
            {
                return input;
            }
        }
    }
}
