using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WPhoneApp
{
    class SharedState
    {
        public ObservableCollection<Site> sites { get; set; }

        public Site selectedSite { get; set; }

        public string password { get; set; }

        public byte[] salt { get; set; }

        public byte[] encryptKey { get; set; }

        public byte[] authKey { get; set; }

        public string displayMessage { get; set; }

        public XElement doc { get; set; }

        public CoreDispatcher uiDispatcher { get; set; }

        public SharedState()
        {
            sites = new ObservableCollection<Site>();
            selectedSite = null;
        }

        public async Task<bool> verifyPassword()
        {
            Stream readStream;
            try
            {
                readStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(
                    "pm-data.xml");
            }
            catch
            {
                // Something went wrong opening the file.
                return false;
            }

            try
            {
                doc = XElement.Load(readStream);
            }
            catch
            {
                // Something went wrong parsing XML.
                return false;
            }

            // CoreWindow coreWindow = CoreWindow.GetForCurrentThread(); // This only works from the UI thread (which we're not).
            CoreWindow coreWindow = CoreApplication.MainView.CoreWindow;
            uiDispatcher = coreWindow.Dispatcher;

            // Get the salt.
            string saltBase64 = doc.Element("salt").Value;
            IBuffer saltBuffer = CryptographicBuffer.DecodeFromBase64String(saltBase64);
            byte[] saltTemp;
            CryptographicBuffer.CopyToByteArray(saltBuffer, out saltTemp);
            salt = saltTemp;

            // Derive and save keys.
            byte[] encryptKeyTemp;
            byte[] authKeyTemp;
            Crypto.deriveKeys(password, salt, out encryptKeyTemp, out authKeyTemp);
            encryptKey = encryptKeyTemp;
            authKey = authKeyTemp;

            // Check one entry.
            string firstName = doc.Element("site").Element("name").Value;
            try
            {
                Crypto.decrypt(firstName);
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("HMAC verification failed."))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
            
            // If we get here, password is verified.
            return true;
        }


        public async Task<bool> FileExists(StorageFolder folder, string fileName)
        {
            try { StorageFile file = await folder.GetFileAsync(fileName); }
            catch { return false; }
            return true;
        }

        public async void loadDataAsync()
        {
            // Clear site list.
            sites.Clear();

            // Load sites.
            IEnumerable<XElement> sitesList = doc.Elements("site");
            foreach (XElement siteElement in sitesList)
            {
                Site thisSite = new Site();
                thisSite.name = siteElement.Element("name").Value;
                thisSite.url = siteElement.Element("url").Value;
                thisSite.user = siteElement.Element("user").Value;
                thisSite.password = siteElement.Element("password").Value;
                thisSite.notes = siteElement.Element("notes").Value;

                // Since this updates the UI, we have to run it on the UI thread.
                await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { sites.Add(thisSite); });
            }
        }


        public async Task<bool> updateViaBluetooth(object sender, RoutedEventArgs e, TextBlock statusText)
        {
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
                //StreamReader sr = new StreamReader(inputStream.AsStreamForRead(), Encoding.UTF8);
                //StreamWriter sw = new StreamWriter(outputStream.AsStreamForWrite(), Encoding.UTF8);

                // Announce ourselves.
                await StreamHelperWP.Write(outputStream, "hello password manager client v1.0\n");

                // Wait for introduction from server.
                string textFromServer = await StreamHelperWP.ReadLine(inputStream);
                if (!textFromServer.Equals("hello password manager server v1.0"))
                {
                    await StreamHelperWP.Write(outputStream, "error: unknown protocol\n");
                    await StreamHelperWP.Write(outputStream, "goodbye");
                    socket.Dispose();
                    statusText.Text = "error: unknown protocol from server";
                    return false;
                }

                // Read information from server.
                textFromServer = await StreamHelperWP.ReadLine(inputStream); // guid
                textFromServer = await StreamHelperWP.ReadLine(inputStream); // friendly name
                textFromServer = await StreamHelperWP.ReadLine(inputStream); // last modified utc
                
                // Figure out if we want this update here.

                // Request latest data.
                await StreamHelperWP.Write(outputStream, "get latest version\n");

                // Figure out size of file.
                textFromServer = await StreamHelperWP.ReadLine(inputStream);
                char[] spaceDelimiter = { ' ' };
                string[] tokens = textFromServer.Split(spaceDelimiter);
                uint fileBytes = (uint) int.Parse(tokens[2]);
                IBuffer tempBuffer = new Windows.Storage.Streams.Buffer((uint)fileBytes);
                uint bytesRead = 0;
                StringBuilder sb = new StringBuilder();
                while (bytesRead < fileBytes)
                {
                    IBuffer fileBuffer = await inputStream.ReadAsync(tempBuffer, fileBytes - bytesRead,
                        InputStreamOptions.Partial);
                    sb.Append(CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, fileBuffer));
                    string tempStr = sb.ToString();
                    int tsLen = tempStr.Length;
                    bytesRead += fileBuffer.Length;
                }
                textFromServer = await StreamHelperWP.ReadLine(inputStream); // txfer complete

                // Close the connection.
                await StreamHelperWP.Write(outputStream, "goodbye\n");
                socket.Dispose();

                // Save the file.
                Stream writeStream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(
                    "pm-data.xml", CreationCollisionOption.ReplaceExisting);
                StreamWriter fileWriter = new StreamWriter(writeStream, Encoding.UTF8);
                fileWriter.Write(sb.ToString());
                fileWriter.Flush();
                fileWriter.Dispose();
                return true;
            }
            else
            {
                statusText.Text = "Unable to connect.";
                return false;
            }
        }


    }
}
