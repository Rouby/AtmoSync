using AtmoSync.Shared;
using AtmoSync.Shared.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
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
    public sealed partial class ServerPage : Page, IServer
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
            Model.PropertyChanged += ModelPropertyChanged;
            (Model.SoundFiles as INotifyCollectionChanged).CollectionChanged += SoundsListChanged;
        }

        private void SoundsListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
            if (e.NewItems != null)
                foreach (INotifyPropertyChanged newItem in e.NewItems)
                    newItem.PropertyChanged += SoundFileChanged;
            if (e.OldItems != null)
                foreach (INotifyPropertyChanged oldItem in e.OldItems)
                    oldItem.PropertyChanged -= SoundFileChanged;
        }

        private void SoundFileChanged(object sender, PropertyChangedEventArgs e)
        {
            var sound = (Sound)sender;

            if (new[]
            {
                nameof(Sound.File),
                nameof(Sound.ServerName),
                nameof(Sound.ClientName),
                nameof(Sound.Volume),
                nameof(Sound.Loop),
                nameof(Sound.Sync)
            }.Contains(e.PropertyName))
            {
                if (sound.Sync)
                {
                    sound.SyncsOutstanding = clients.Count;
                    sound.IsSynced = clients.Count == 0;
                    foreach (var client in clients)
                    {
                        client.Value.EnqueueMessage(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = sound }, () =>
                        {
                            var ignore2 = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                sound.DecrementSyncsOutstanding();
                            });
                        });
                    }
                }
                else
                {
                    foreach (var client in clients)
                    {
                        client.Value.EnqueueMessage(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = new Sound { Id = sound.Id } });
                    }
                }
            }
            else if (e.PropertyName == nameof(Sound.Status))
            {
                Message msg = null;
                switch (sound.Status)
                {
                    case Status.Playing:
                        msg = new PlaySoundMessage { Timestamp = DateTimeOffset.Now, SoundId = sound.Id };
                        break;
                    case Status.Paused:
                        msg = new PauseSoundMessage { Timestamp = DateTimeOffset.Now, SoundId = sound.Id };
                        break;
                    case Status.Stopped:
                        msg = new StopSoundMessage { Timestamp = DateTimeOffset.Now, SoundId = sound.Id };
                        break;
                }

                foreach (var client in clients)
                {
                    client.Value.EnqueueMessage(msg);
                }
            }
        }

        private void ModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServerViewModel.SoundFiles))
            {

            }
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
                listener.Control.QualityOfService = SocketQualityOfService.Normal;
                listener.ConnectionReceived += HandleClientAsync;
                await listener.BindServiceNameAsync("56779");
                Model.Listening = true;

                //while (listener != null) { await Task.Delay(1000); }
            }
            catch (Exception e)
            {
                var msg = new MessageDialog(e.Message);
                await msg.ShowAsync();
                Model.Listening = false;
            }
            finally
            {
                //listener?.Dispose();
                //listener = null;
                //Model.Listening = false;
            }
        }


        async void HandleClientAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var id = Guid.NewGuid();
            var client = ClientHandler.CreateNew(args.Socket, this);
            clients.TryAdd(id, client);

            foreach (var sound in Model.SoundFiles.Where(s => s.Sync))
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    sound.IncrementSyncsOutstanding();
                    sound.IsSynced = false;
                });
                client.EnqueueMessage(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = sound }, () =>
                {
                    var ignore2 = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        sound.DecrementSyncsOutstanding();
                    });
                });
            }

            try
            {
                await client.Run();
            }
            catch { }

            clients.TryRemove(id, out client);
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

                await AddFiles(items.Cast<StorageFile>());
            }
        }

        private async void AddNewSound(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".ogg");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            if (files?.Any() ?? false)
            {
                await AddFiles(files);
            }
        }

        async Task AddFiles(IEnumerable<StorageFile> files)
        {
            foreach (var file in files)
            {
                if (new[] { ".mp3", ".ogg", ".wav" }.Contains(file.FileType))
                {
                    Model.SoundFiles.Add(new Sound
                    {
                        Id = Guid.NewGuid(),
                        ServerName = file.DisplayName,
                        Volume = 1,
                        File = file.Path
                    });
                }
            }

            await Model.SaveSoundFilesAsync(string.IsNullOrWhiteSpace(serverTextBox.Text) ? "direct" : serverTextBox.Text);
        }

        public async Task<IStorageFile> GetSoundFileAsync(Guid id)
        {
            return await StorageFile.GetFileFromPathAsync(Model.SoundFiles.FirstOrDefault(s => s.Id.Equals(id)).File);
        }

        private async void RemoveSound(object sender, EventArgs e)
        {
            if (Model.SoundFiles.Contains(sender))
            {
                Model.SoundFiles.Remove((Sound)sender);
                await Model.SaveSoundFilesAsync(string.IsNullOrWhiteSpace(serverTextBox.Text) ? "direct" : serverTextBox.Text);
            }
        }
    }
}
