using AtmoSync.Shared;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
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
    public sealed partial class ClientPage : Page, IClient
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
                var ignore = DirectConnectAsync(serverAddress);
            }
            else
            {
                var ignore = ConnectViaPunchServerAsync();
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

                await DirectConnectAsync($"{response.ServerAddress}:{response.ServerPort}");

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
                port = "56779";
            }

            try
            {
                var socket = new StreamSocket();
                await socket.ConnectAsync(new HostName(name), port);

                connection = socket;
                Model.Connected = true;

                var msg = new MessageDialog("connected to server");
                await msg.ShowAsync();

                var server = ServerHandler.CreateNew(socket, this);

                var ignore = Task.Factory.StartNew(() => server.Run());
            }
            catch (Exception e)
            {
                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();

                Model.Connected = false;
            }
        }

        async Task<bool> IClient.SoundExistsAsync(Guid id)
        {
            foreach (var sound in Model.SoundFiles.Where(s => s.Id.Equals(id)))
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(sound.File);
                    return true;
                }
                catch { }
            }
            return false;
        }

        void IClient.AddSound(Sound sound)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Model.SoundFiles.Add(sound);
            });
        }

        void IClient.SyncSound(Guid id, Sound sound)
        {
            
        }

        void IClient.RemoveSound(Guid id)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Model.SoundFiles.Remove(Model.SoundFiles.First(s => s.Id.Equals(id)));
            });
        }
    }
}
