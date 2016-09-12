using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PasswordManager
{
    public static class Switcher
    {
        public static MainWindow mainWindow;

        public static void Switch(UserControl newPage)
        {
            mainWindow.Navigate(newPage);
        }
    }
}
