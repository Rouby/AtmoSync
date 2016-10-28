using AtmoSync.Shared;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AtmoSync.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ClientPage : Page
    {
        ClientViewModel Model { get; set; }

        StreamSocket punchServer;
        StreamSocket connection;

        public ClientPage()
        {
            InitializeComponent();
            DataContext = Model = new ClientViewModel();
        }

        void ConnectServerTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        void InitConnectionTapped(object sender, TappedRoutedEventArgs e)
        {
            IPAddress address;
            var serverAddress = serverTextBox.Text.Contains(":")
                ? serverTextBox.Text.Split(':')[0]
                : serverTextBox.Text;
            if (IPAddress.TryParse(serverAddress, out address))
            {
                DirectConnectAsync(serverAddress);
            }
            else
            {
                ConnectViaPunchServerAsync();
            }
            connectServerFlyout.Hide();
        }

        async Task ConnectViaPunchServerAsync()
        {
            try
            {
                punchServer = new StreamSocket();
                await punchServer.ConnectAsync(new HostName(Model.Settings.PunchServerAddress), Model.Settings.PunchServerPort);

                var streamOut = punchServer.OutputStream.AsStreamForWrite();
                var writer = new StreamWriter(streamOut);
                await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchRequest { ConnectTo = serverTextBox.Text }));
                await writer.FlushAsync();

                var streamIn = punchServer.InputStream.AsStreamForRead();
                var reader = new StreamReader(streamIn);
                var response = JsonConvert.DeserializeObject<PunchResponse>(await reader.ReadLineAsync());

                if (!response.Valid)
                    throw new Exception(response.Message ?? "Could not connect to remote server.");

                //var serverSocket = new StreamSocket();
                //await serverSocket.ConnectAsync(new HostName(response.ServerAddress), response.ServerPort);

                //Model.ServerSocket = serverSocket;
            }
            catch (Exception e)
            {
                punchServer.Dispose();
                punchServer = null;

                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
            }
        }

        async Task DirectConnectAsync(string address)
        {
            string name;
            string port;
            if (address.Contains(":"))
            {
                name = address.Split(':')[0];
                port = address.Split(':')[1];
            }
            else
            {
                name = address.Split(':')[0];
                port = "34512";
            }

            try
            {
                var socket = new StreamSocket();
                await socket.ConnectAsync(new HostName(name), port);

                connection = socket;
                Model.Connected = true;

                var msg = new MessageDialog("connected to server");
                await msg.ShowAsync();
            }
            catch (Exception e)
            {
                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();

                Model.Connected = false;
            }
        }
    }
}
