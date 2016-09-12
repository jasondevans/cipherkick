using System;
using System.Text;
using System.Threading;
using System.Windows;

using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Bluetooth.AttributeIds;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager
{
    /// <summary>
    /// Interaction logic for ChangeMasterPage.xaml
    /// </summary>
    public partial class BluetoothUpdatePage : StandardUserControl
    {
        private bool bluetoothManagerStarted = false;

        public BluetoothUpdatePage()
        {
            InitializeComponent();
        }

        private void updateViaBluetoothButton_Click(object sender, RoutedEventArgs e)
        {
            if (bluetoothManagerStarted == false)
            {
                Thread thread = new Thread(new ThreadStart(this.manageBluetooth));
                thread.IsBackground = true;
                bluetoothManagerStarted = true;
                thread.Start();
            }
        }

        private void manageBluetooth()
        {
            Guid serviceIdUuid = new Guid("{efd220d0-7443-43f8-9a0a-e61f833c1006}");
            Guid serviceClassIdUuid = serviceIdUuid;

            ServiceRecordBuilder builder = new ServiceRecordBuilder();
            builder.AddServiceClass(serviceClassIdUuid);
            builder.ServiceName = "Password Manager";
            builder.AddCustomAttribute(UniversalAttributeId.ServiceId, ElementType.Uuid128, serviceIdUuid);
            ServiceRecord serviceRecord = builder.ServiceRecord;

            var listener = new BluetoothListener(serviceClassIdUuid, serviceRecord);
            listener.Start();

            while (true)
            {
                try
                {
                    // Wait for client connection.
                    setStatus("waiting for client connection...");
                    BluetoothClient btClient = listener.AcceptBluetoothClient();
                    setStatus("client connected...");

                    // Set up streams.
                    Stream s = btClient.GetStream();
                    // StreamWriter sw = new StreamWriter(stream);
                    // StreamReader sr = new StreamReader(stream);

                    // Wait for client to introduce themselves.
                    string textFromClient = StreamHelper.ReadLine(s);
                    if (!textFromClient.Equals("hello password manager client v1.0"))
                    {
                        setStatus("error: unknown protocol. disconnected.");
                        StreamHelper.Write(s, "error: unknown protocol\n");
                        StreamHelper.Write(s, "goodbye\n");
                        s.Dispose();
                        btClient.Dispose();
                        continue;
                    }

                    // Introduce ourselves.
                    StreamHelper.Write(s, "hello password manager server v1.0\n");

                    // Send metadata.
                    WAppCpp.Util util = WAppCpp.Util.Instance;
                    WAppCpp.Metainfo metadata = util.getMetadata();
                    StreamHelper.Write(s, "guid: " + metadata.guid + "\n");
                    StreamHelper.Write(s, "friendly name: " + metadata.friendlyName + "\n");
                    StreamHelper.Write(s, "last modified utc: " + metadata.lastModifiedUtc + "\n");

                    // Get client requests.
                    bool processingRequests = true;
                    String exceptionMsg = null;
                    while (processingRequests)
                    {
                        textFromClient = StreamHelper.ReadLine(s);
                        if (textFromClient.Equals("get latest version"))
                        {
                            setStatus("latest data requested...");
                            String exportText = util.getExportData(SharedState.password);

                            /*StringBuilder sb = new StringBuilder();
                            sb.Append("<?xml version=\"1.0\"?>\n");
                            sb.Append("<password-manager>\n");
                            sb.Append("<salt>alkasjdflaksdjflaksjdflkasjdfljasdfkj</salt>\n");
                            sb.Append("<metadata>\n");
                            sb.Append("</metadata>\n");
                            for (int i = 0; i < 1000; i++)
                            {
                                sb.Append("This right here is text line number " + (i + 1) + ".\n");
                            }
                            sb.Append("</password-manager>\n");
                            String exportText = sb.ToString();*/

                            int etLength = exportText.Length;
                            byte[] exportBytes = Encoding.UTF8.GetBytes(exportText);
                            StreamHelper.Write(s, "ok bytes " + exportBytes.Length + "\n");
                            s.Write(exportBytes, 0, exportBytes.Length); s.Flush();
                            StreamHelper.Write(s, "txfer complete\n");
                            setStatus("data sent successfully.");
                        }
                        else if (textFromClient.StartsWith("error:"))
                        {
                            setStatus(textFromClient);
                            processingRequests = false;
                            exceptionMsg = textFromClient;
                        }
                        else if (textFromClient.Equals("goodbye"))
                        {
                            setStatus("client closed connection.");
                            processingRequests = false;
                        }
                    }

                    // Close the connection.
                    s.Dispose();
                    btClient.Dispose();

                    // Throw exception if there an error.
                    if (exceptionMsg != null)
                    {
                        setStatus("exception occurred: " + exceptionMsg);
                        return;
                    }
                }
                catch (Exception e) 
                {
                    // Handle exception here.
                    setStatus("exception occurred: " + e.ToString());
                    return;
                }
            }
        }

        private void setStatus(String status)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            { bluetoothStatusBlock.Text = status; }));
        }
    }
}
