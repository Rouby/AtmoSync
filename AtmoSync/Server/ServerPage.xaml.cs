using AtmoSync.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
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
        StreamReader punchIn;
        StreamWriter punchOut;


        StreamSocketListener listener;
        ConcurrentDictionary<Guid, ClientHandler> clients = new ConcurrentDictionary<Guid, ClientHandler>();

        public ServerPage()
        {
            InitializeComponent();
            DataContext = Model = new ServerViewModel();
        }

        void HostServerTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        void StartServerTapped(object sender, TappedRoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            if (punshThroughSwitch.IsOn)
            {
                RegisterOnPunchServerAsync();
                Model.LoadSoundFilesAsync(serverTextBox.Text);
            }
            else
            {
                CreateListenerAsync();
                Model.LoadSoundFilesAsync("direct");
            }
            startServerFlyout.Hide();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        async Task RegisterOnPunchServerAsync()
        {
            try
            {
                punchServer = new StreamSocket();
                await punchServer.ConnectAsync(new HostName(Model.Settings.PunchServerAddress), Model.Settings.PunchServerPort);

                punchIn = new StreamReader(punchServer.InputStream.AsStreamForRead());
                punchOut = new StreamWriter(punchServer.OutputStream.AsStreamForWrite());

                await punchOut.WriteLineAsync(JsonConvert.SerializeObject(new PunchRequest { ServerAlias = serverTextBox.Text }));
                await punchOut.FlushAsync();

                var response = JsonConvert.DeserializeObject<PunchResponse>(await punchIn.ReadLineAsync());

                if (!response.Valid)
                    throw new Exception(response.Message ?? "Could not establish server alias.");

                var cts = new CancellationTokenSource();
                var tasks = new[] { CreateListenerAsync(), CreateHeartbeatPunchServerAsync(cts.Token) };

                await Task.WhenAny(tasks);

                cts.Cancel();
            }
            catch (Exception e)
            {
                punchIn?.Dispose();
                punchOut?.Dispose();
                punchServer.Dispose();
                punchIn = null;
                punchOut = null;
                punchServer = null;

                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
            }
        }

        async Task CreateHeartbeatPunchServerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && punchServer != null)
            {
                await punchOut.WriteLineAsync(JsonConvert.SerializeObject(new PunchHeartbeat()));
                await punchOut.FlushAsync();

                var response = JsonConvert.DeserializeObject<PunchHeartbeat>(await punchIn.ReadLineAsync());

                await Task.Delay(10000);
            }
        }

        async Task CreateListenerAsync()
        {
            try
            {
                listener = new StreamSocketListener();
                listener.ConnectionReceived += HandleClientAsync;
                await listener.BindServiceNameAsync("56779");
                Model.Listening = true;

                while (listener != null) { await Task.Delay(1000); }
            }
            catch (Exception e)
            {
                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
            }
            finally
            {
                listener?.Dispose();
                listener = null;
                Model.Listening = false;
            }
        }


        async void HandleClientAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var id = Guid.NewGuid();
            var client = ClientHandler.CreateNew(args.Socket);
            clients.TryAdd(id, client);

            await client.Run();

            clients.TryRemove(id, out client);
        }

        private void TappedSoundFile(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                foreach (StorageFile item in items)
                {
                    if (new[] { ".mp3", ".ogg", ".wav" }.Contains(item.FileType))
                    {
                        Model.SoundFiles.Add(new Sound
                        {
                            Id = Guid.NewGuid(),
                            ServerName = item.DisplayName,
                            Volume = 1,
                            File = item.Path
                        });
                    }
                }
            }
        }
    }
}
