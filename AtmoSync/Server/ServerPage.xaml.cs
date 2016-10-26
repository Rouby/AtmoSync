using AtmoSync.Shared;
using AtmoSync.Shared.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AtmoSync.Server
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ServerPage : Page
    {
        ServerViewModel Model { get; set; }

        StreamSocket punchServer;
        StreamSocketListener listener;
        ConcurrentDictionary<Guid, StreamSocket> clients = new ConcurrentDictionary<Guid, StreamSocket>();

        public ServerPage()
        {
            InitializeComponent();
            DataContext = Model = new ServerViewModel();

        }

        private void StartServerTapped(object sender, TappedRoutedEventArgs e)
        {
            if (punshThroughSwitch.IsOn)
            {
                RegisterOnPunchServerAsync();
            }
            else
            {
                CreateListenerAsync();
            }
        }

        async Task RegisterOnPunchServerAsync()
        {
            try
            {
                punchServer = new StreamSocket();
                await punchServer.ConnectAsync(new HostName(Model.Settings.PunchServerAddress), Model.Settings.PunchServerPort);

                var streamOut = punchServer.OutputStream.AsStreamForWrite();
                var writer = new StreamWriter(streamOut);
                await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchRequest { ServerAddress = serverAlias.Text }));
                await writer.FlushAsync();

                var streamIn = punchServer.InputStream.AsStreamForRead();
                var reader = new StreamReader(streamIn);
                var response = JsonConvert.DeserializeObject<PunchResponse>(await reader.ReadLineAsync());

                if (!response.Valid)
                    throw new Exception(response.Message ?? "Could not establish server alias.");

                await CreateListenerAsync();
            }
            catch (Exception e)
            {
                punchServer.Dispose();
                punchServer = null;

                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
            }
        }

        async Task CreateListenerAsync()
        {
            try
            {
                listener = new StreamSocketListener();
                listener.ConnectionReceived += ListenerConnectionReceived;
            }
            catch (Exception e)
            {
                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
            }
        }

        void ListenerConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            HandleClientAsync(args.Socket);
        }

        async Task HandleClientAsync(StreamSocket socket)
        {
            var id = Guid.NewGuid();
            clients.TryAdd(id, socket);

            try
            {
                var streamIn = punchServer.InputStream.AsStreamForRead();
                var reader = new StreamReader(streamIn);
                var streamOut = punchServer.OutputStream.AsStreamForWrite();
                var writer = new StreamWriter(streamOut);
                while (true)
                {
                    var json = await reader.ReadLineAsync();
                    {
                        var msg = JsonConvert.DeserializeObject<SyncSoundMessage>(json);
                        if (msg != null)
                        {

                        }
                    }
                    {
                        var msg = JsonConvert.DeserializeObject<PlaySoundMessage>(json);
                        if (msg != null)
                        {

                        }
                    }
                    {
                        var msg = JsonConvert.DeserializeObject<StopSoundMessage>(json);
                        if (msg != null)
                        {

                        }
                    }
                }
            }
            finally
            {
                clients.TryRemove(id, out socket);
            }
        }

        private void ServerPageLoaded(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
    }
}
