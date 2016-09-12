using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Xml.Linq;


namespace PasswordManager
{
    public partial class MainWindow : Window
    {
        // A list of sites for purposes of hotkey matching.
        List<WAppCpp.Site> siteList;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        int WM_HOTKEY = 0x0312;
        
        public MainWindow()
        {
            InitializeComponent();
            Switcher.mainWindow = this;
            Switcher.Switch(SharedState.welcomePage);
        }

        public void Navigate(UserControl nextPage)
        {
            this.Content = nextPage;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // Put stuff here that should execute once the window has loaded.
        }

        public void postPasswordInitialize()
        {
            // Update hotkey matching info;
            updateHotkeyMatchInfo();

            // Add a hook to receive window messages (to potentially get notified when hotkeys are pressed).
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);

            // If enabled, start handling hotkeys.
            if (SharedState.hotkeysEnabled)
            {
                startHotkeyHandling();
            }

            // Start a thread to check for idle timeout.
            Thread idleTimeoutThread = new Thread(() =>
            {
                uint idleMillis = 0;
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
                lastInputInfo.dwTime = 0;

                while (true)
                {
                    // Sleep for 1 minute.
                    Thread.Sleep(1 * 60 * 1000);

                    // Get the number of idle milliseconds.
                    uint envTicks = (uint)Environment.TickCount;
                    if (GetLastInputInfo(ref lastInputInfo))
                    {
                        uint lastInputTick = lastInputInfo.dwTime;
                        idleMillis = envTicks - lastInputTick;
                    }

                    // If we're checking idle timeout and it's been exceeded, handle it.
                    if (SharedState.idleTimeoutEnabled && idleMillis > SharedState.idleTimeoutMillis)
                    {
                        // Close the application.
                        App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        { App.Current.Shutdown(); }));
                    }
                }
            });
            idleTimeoutThread.IsBackground = true;
            idleTimeoutThread.Start();
        }

        public void updateHotkeyMatchInfo()
        {
            // Get a site list, for purposes of hotkey matching.
            siteList = WAppCpp.Util.Instance.getSiteList();
            Regex url2Regex =
                new Regex(@"^\s*(?:https*://)?(?:[-A-Za-z0-9]+\.)*(?<url2>(?:[-A-Za-z0-9]+\.)(?:[-A-Za-z0-9]+))(?:$|[^-A-Za-z0-9])");
            Regex url3Regex =
                new Regex(@"^\s*(?:https*://)?(?:[-A-Za-z0-9]+\.)*(?<url3>(?:[-A-Za-z0-9]+\.){2}(?:[-A-Za-z0-9]+))(?:$|[^-A-Za-z0-9])");
            Regex name1Regex =
              new Regex(@"^\s*(?i)(?:(a|the)\s+)?(?-i)\s*(?<name1>.*)$");
            String[] delimiters = { " -", " (", " )", " /", " [", " ]", " :" };
            foreach (WAppCpp.Site site in siteList)
            {
                if (site.url == null || site.url.Equals(""))
                {
                    site.canonicalUrl2 = "";
                    site.canonicalUrl3 = "";
                }
                else
                {
                    MatchCollection mc2 = url2Regex.Matches(site.url);
                    MatchCollection mc3 = url3Regex.Matches(site.url);
                    String url2 = null;
                    String url3 = null;
                    if (mc2.Count > 0) url2 = mc2[0].Groups["url2"].Value;
                    if (mc3.Count > 0) url3 = mc3[0].Groups["url3"].Value;
                    if (url3 == null && url2 != null) url3 = url2;
                    site.canonicalUrl2 = url2 == null ? "" : url2.ToLower();
                    site.canonicalUrl3 = url3 == null ? "" : url3.ToLower();
                }
                if (site.name == null || site.name.Equals(""))
                {
                    site.canonicalName1 = "";
                    site.canonicalName2 = "";
                    site.canonicalName3 = "";
                    site.canonicalName4 = "";
                }
                else
                {
                    MatchCollection mc = name1Regex.Matches(site.name);
                    String cname1 = null;
                    if (mc.Count > 0) cname1 = mc[0].Groups["name1"].Value;
                    site.canonicalName1 = cname1 == null ? "" : cname1.ToLower();
                    site.canonicalName2 = site.canonicalName1.Split(delimiters, StringSplitOptions.None)[0];
                    site.canonicalName3 = site.canonicalName1.Replace(" ", String.Empty);
                    site.canonicalName4 = site.canonicalName2.Replace(" ", String.Empty);
                }
            }
        }

        public void startHotkeyHandling()
        {
            // Register global hotkeys.
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            int hotkeyIdU = 1;
            int hotkeyIdP = 2;
            int hotkeyIdV = 3;
            uint fsModifiers = 0x0002 /* MOD_CONTROL */ | 0x0004 /* MOD_SHIFT */ | 0x4000 /* MOD_NOREPEAT */;
            // uint fsWinModifier = 0x0008 /* MOD_WIN */ | 0x4000 /* MOD_NOREPEAT */;
            uint virtualKeyU = 0x55; // U key
            uint virtualKeyP = 0x50; // P key
            uint virtualKeyV = 0x56; // V key
            bool registerStatus = RegisterHotKey(windowHandle, hotkeyIdU, fsModifiers, virtualKeyU);
            registerStatus = RegisterHotKey(windowHandle, hotkeyIdP, fsModifiers, virtualKeyP);
            registerStatus = RegisterHotKey(windowHandle, hotkeyIdV, fsModifiers, virtualKeyV);
        }

        public void stopHotkeyHandling()
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            int hotkeyIdU = 1;
            int hotkeyIdP = 2;
            int hotkeyIdV = 3;
            UnregisterHotKey(windowHandle, hotkeyIdU);
            UnregisterHotKey(windowHandle, hotkeyIdP);
            UnregisterHotKey(windowHandle, hotkeyIdV);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && SharedState.hotkeysEnabled && (wParam.ToInt32() == 1 || wParam.ToInt32() == 2))
            {
                // Get the active window.
                IntPtr activeWindow = GetForegroundWindow();

                // Get the active window's title text.
                String windowTitle = "";
                int length = GetWindowTextLength(activeWindow);
                if (length > 0)
                {
                    StringBuilder builder = new StringBuilder(length);
                    GetWindowText(activeWindow, builder, length + 1);
                    windowTitle = builder.ToString();
                }

                // Standardize the window title.
                if (windowTitle == null) windowTitle = "";
                windowTitle = windowTitle.ToLower();
                String ieString = "internet explorer";
                int ieIndex = windowTitle.IndexOf(ieString);
                if (ieIndex != -1)
                {
                    windowTitle = windowTitle.Substring(0, ieIndex) +
                        windowTitle.Substring(ieIndex + ieString.Length);
                }
                String meString = "microsoft edge";
                int meIndex = windowTitle.IndexOf(meString);
                if (meIndex != -1)
                {
                    windowTitle = windowTitle.Substring(0, meIndex) +
                        windowTitle.Substring(meIndex + meString.Length);
                }
                String gcString = "google chrome";
                int gcIndex = windowTitle.IndexOf(gcString);
                if (gcIndex != -1)
                {
                    windowTitle = windowTitle.Substring(0, gcIndex) +
                        windowTitle.Substring(gcIndex + gcString.Length);
                }
                String mfString = "mozilla firefox";
                int mfIndex = windowTitle.IndexOf(mfString);
                if (mfIndex != -1)
                {
                    windowTitle = windowTitle.Substring(0, mfIndex) +
                        windowTitle.Substring(mfIndex + mfString.Length);
                }

                // See if we have a url match.
                WAppCpp.Site matchingSite = null;
                if (!String.IsNullOrEmpty(windowTitle))
                {
                    foreach (WAppCpp.Site thisSite in siteList)
                    {
                        if (windowTitle.IndexOf(thisSite.canonicalName1) != -1 && !String.IsNullOrEmpty(thisSite.canonicalName1))
                        {
                            matchingSite = thisSite;
                            break;
                        }
                    }
                    if (matchingSite == null) foreach (WAppCpp.Site thisSite in siteList)
                    {
                        if (windowTitle.IndexOf(thisSite.canonicalName2) != -1 && !String.IsNullOrEmpty(thisSite.canonicalName2))
                        {
                            matchingSite = thisSite;
                            break;
                        }
                    }
                    if (matchingSite == null) foreach (WAppCpp.Site thisSite in siteList)
                    {
                        if (windowTitle.IndexOf(thisSite.canonicalName3) != -1 && !String.IsNullOrEmpty(thisSite.canonicalName3))
                        {
                            matchingSite = thisSite;
                            break;
                        }
                    }
                    if (matchingSite == null) foreach (WAppCpp.Site thisSite in siteList)
                    {
                        if (windowTitle.IndexOf(thisSite.canonicalName4) != -1 && !String.IsNullOrEmpty(thisSite.canonicalName4))
                        {
                            matchingSite = thisSite;
                            break;
                        }
                    }

                    // Add a delay (for some reason, in Chrome without a delay the input doesn't show up).
                    // This is a bit of a hack.  No need to do this for OCR since that takes plenty of time.
                    Thread.Sleep(200);
                }

                if (matchingSite == null)
                {
                    // Take a screenshot of the active window.
                    Rect rect = new Rect();
                    GetWindowRect(activeWindow, ref rect);
                    System.Drawing.Rectangle bounds = new System.Drawing.Rectangle(
                        rect.Left, rect.Top,
                        600, // rect.Right - rect.Left
                        100); // rect.Bottom - rect.Top
                    Bitmap bitmap = ScreenCapture.CaptureWindow(bounds);

                    // Save the screenshot to a file.
                    //bitmap.Save("c:\\Users\\jason\\Documents\\Projects\\PasswordManager\\screenshot.bmp", ImageFormat.Bmp);

                    // Do OCR.
                    String ocrResult = WAppCpp.Util.Instance.doOCR(bitmap);

                    // See if there's a domain name.
                    Regex regex = new Regex("(?<url>(?:[-a-z0-9]+\\.)(?:com|org|net|edu))");
                    MatchCollection mc = regex.Matches(ocrResult);
                    String url = null;
                    if (mc.Count > 0)
                    {
                        url = mc[0].Groups["url"].Value;
                    }

                    // See if we have a url match.
                    if (url != null)
                    {
                        foreach (WAppCpp.Site thisSite in siteList)
                        {
                            if (thisSite.canonicalUrl2.Equals(url))
                            {
                                // For now, just take the first matching site.
                                matchingSite = thisSite;
                                break;
                            }
                        }
                    }
                }

                // If we have a match, get the site and send requested input.
                if (matchingSite != null)
                {
                    WAppCpp.Site fullMatchingSite = WAppCpp.Util.Instance.getSite(matchingSite);
                    if (wParam.ToInt32() == 1)
                    {
                        WindowsInput.InputSimulator.SimulateTextEntry(fullMatchingSite.user);
                    }
                    else // (wParam.ToInt32() == 2)
                    {
                        WindowsInput.InputSimulator.SimulateTextEntry(fullMatchingSite.password);
                    }
                }
            }
            else if (msg == WM_HOTKEY && SharedState.hotkeysEnabled && (wParam.ToInt32() == 3))
            {
                WindowsInput.InputSimulator.SimulateTextEntry(Clipboard.GetText());
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            stopHotkeyHandling();
        }

    }
}
