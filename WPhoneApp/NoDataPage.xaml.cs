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
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Core;


namespace WPhoneApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoDataPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private SharedState sharedState;
        private CoreDispatcher uiDispatcher;

        public NoDataPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            sharedState = (App.Current.Resources["sharedState"] as SharedState);
            uiDispatcher = sharedState.uiDispatcher;
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

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
            
            // Navigate to the password page.
            if (updateSucceeded) this.Frame.Navigate(typeof(PasswordPage));
            
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
                Stream inputStream = socket.InputStream.AsStreamForRead();
                Stream outputStream = socket.OutputStream.AsStreamForWrite();
                inputStream.ReadTimeout = 20 * 1000;  // Set timeout to 20 seconds.
                LineReader sr = new LineReader(inputStream, Encoding.UTF8);
                StreamWriter sw = new StreamWriter(outputStream, Encoding.UTF8);

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

                // TODO: Real issues throughout bluetooth update.  Most recent code is in this file, not BluetoothUpdatePage.

                // Figure out size of file.
                textFromServer = sr.ReadLine();
                char[] spaceDelimiter = { ' ' };
                string[] tokens = textFromServer.Split(spaceDelimiter);
                int numBytes = int.Parse(tokens[2]);

                int bytesRead = 0;
                byte[] fileBytes = new byte[numBytes];
                try
                {
                    while (bytesRead < numBytes)
                    {
                        if (bytesRead % 10000 == 0)
                        {
                            int q = 0;
                        }
                        fileBytes[bytesRead] = sr.ReadByte();
                        bytesRead++;
                    }
                    //byte[] fileBytes = sr.ReadBytes(numBytes);
                }
                catch (Exception ex)
                {
                    int p = 0;
                }

                string fileStr = System.Text.Encoding.UTF8.GetString(fileBytes, 0, numBytes);
                textFromServer = sr.ReadLine(); // txfer complete

                // Close the connection.
                sw.WriteLine("goodbye"); sw.Flush();
                socket.Dispose();

                // Save the file.
                Stream writeStream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(
                    "pm-data.xml", CreationCollisionOption.ReplaceExisting);
                StreamWriter fileWriter = new StreamWriter(writeStream, Encoding.UTF8);
                fileWriter.Write(fileStr);
                fileWriter.Flush();
                fileWriter.Dispose();

                // Navigate to the password page.
                this.Frame.Navigate(typeof(PasswordPage));
            }
            else
            {
                statusText.Text = "Unable to connect.";
            }
            */
        }

    }
}
