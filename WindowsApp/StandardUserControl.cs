using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace PasswordManager
{
    public class StandardUserControl : UserControl
    {
        public StandardPage sp;
        public WAppCpp.Util util;

        public StandardUserControl()
        {
            sp = (App.Current.Resources["standardPage"] as StandardPage);
            util = WAppCpp.Util.Instance;

            this.PreviewKeyDown += sp.StandardPage_PreviewKeyDown;
            this.PreviewKeyUp += sp.StandardPage_PreviewKeyUp;
            this.Loaded += StandardUserControl_Loaded;

            this.Focusable = true;
            this.FocusVisualStyle = null;
        }

        void StandardUserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
        }


        public virtual bool childHandleKeyDown(object sender, KeyEventArgs e)
        {
            // Child overrides this method if they want to do anything.
            return false;
        }

    }
}
