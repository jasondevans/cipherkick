using WPhoneApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.UI.Popups;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Text;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.UI.Core;

namespace WPhoneApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BluetoothUpdatePage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private SharedState sharedState;
        private CoreDispatcher uiDispatcher;

        public BluetoothUpdatePage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            sharedState = (App.Current.Resources["sharedState"] as SharedState);
            uiDispatcher = sharedState.uiDispatcher;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void updateViaBluetoothButton_Click(object sender, RoutedEventArgs e)
        {
            bool updateSucceeded = await sharedState.updateViaBluetooth(sender, e, statusText);

            // Figure out what to do here.

            /*
            statusText.Text = "Attempting to connect...";
            
            Guid serviceIdGuid = new Guid("{efd220d0-7443-43f8-9a0a-e61f833c1006}");

            DeviceInformationCollection devInfoCollection 
                = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());

            RfcommDeviceService rfcommSvc = null;
            StreamSocket socket = new StreamSocket();
            foreach (DeviceInformation thisdi in devInfoCollection)
            {
                BluetoothDevice btDev = await BluetoothDevice.FromIdAsync(thisdi.Id);
                IReadOnlyList<RfcommDeviceService> rfcommSvcs = btDev.RfcommServices;
                foreach (RfcommDeviceService thisService in rfcommSvcs)
                {
                    if (thisService.ServiceId.Uuid.Equals(serviceIdGuid))
                    {
                        try
                        {
                            await socket.ConnectAsync(thisService.ConnectionHostName, 
                                thisService.ConnectionServiceName);
                            rfcommSvc = thisService;
                            break;
                        }
                        catch (Exception)
                        {
                            // This didn't succeed.
                        }
                    }
                }
            }

            if (rfcommSvc != null)
            {
                // Update status.
                statusText.Text = "Successfully connected.";

                // Get streams.
                IInputStream inputStream = socket.InputStream;
                IOutputStream outputStream = socket.OutputStream;
                StreamReader sr = new StreamReader(inputStream.AsStreamForRead(), Encoding.UTF8);
                StreamWriter sw = new StreamWriter(outputStream.AsStreamForWrite(), Encoding.UTF8);

                // Announce ourselves.
                sw.WriteLine("hello password manager client v1.0"); sw.Flush();

                // Wait for introduction from server.
                string textFromServer = sr.ReadLine();
                if (!textFromServer.Equals("hello password manager server v1.0"))
                {
                    sw.WriteLine("error: unknown protocol");
                    sw.WriteLine("goodbye");
                    socket.Dispose();
                    statusText.Text = "error: unknown protocol from server";
                    return;
                }

                // Read information from server.
                textFromServer = sr.ReadLine(); // guid
                textFromServer = sr.ReadLine(); // friendly name
                textFromServer = sr.ReadLine(); // last modified utc
                // Figure out if we want this update here.

                // Request latest data.
                sw.WriteLine("get latest version"); sw.Flush();

                // Figure out size of file.
                textFromServer = sr.ReadLine();
                char[] spaceDelimiter = { ' ' };
                string[] tokens = textFromServer.Split(spaceDelimiter);
                int fileBytes = int.Parse(tokens[2]);
                IBuffer tempBuffer = new Windows.Storage.Streams.Buffer((uint)fileBytes);
                IBuffer fileBuffer = await inputStream.ReadAsync(tempBuffer, (uint)fileBytes, 
                    InputStreamOptions.Partial);
                string fileStr = 
                    CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, fileBuffer);
                textFromServer = sr.ReadLine(); // txfer complete

                // Close the connection.
                sw.WriteLine("goodbye"); sw.Flush();
                socket.Dispose();
            }
            else
            {
                statusText.Text = "Unable to connect.";
            }
            */
        }
    }
}
