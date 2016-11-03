using AtmoSync.Shared;
using AtmoSync.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking;
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

        readonly Server server;

        public ServerPage()
        {
            InitializeComponent();
            DataContext = Model = new ServerViewModel();
            Model.PropertyChanged += ModelPropertyChanged;
            (Model.SoundFiles as INotifyCollectionChanged).CollectionChanged += SoundsListChanged;

            server = new Server { Interface = this };
            server.Startup += ServerStartup;
            server.Teardown += ServerTeardown;
            server.ClientConnected += ServerClientConnected;
        }

        void ServerClientConnected(object sender, ClientHandler client)
        {
            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var sound in Model.SoundFiles.Where(s => s.Sync))
                {
                    sound.IncrementSyncsOutstanding();
                    client.EnqueueMessage(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = sound }, () =>
                    {
                        var ignoreToo = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, sound.DecrementSyncsOutstanding);
                    });
                }
            });
        }

        void ServerStartup(object sender, EventArgs e)
        {
            Model.Listening = true;
        }

        void ServerTeardown(object sender, EventArgs e)
        {
            Model.Listening = false;
        }

        void SoundsListChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        void SoundFileChanged(object sender, PropertyChangedEventArgs e)
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
                    sound.SyncsOutstanding = server.ConnectedClients;
                    sound.IsSynced = server.ConnectedClients == 0;
                    server.Broadcast(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = sound }, () =>
                    {
                        var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, sound.DecrementSyncsOutstanding);
                    });
                }
                else
                {
                    server.Broadcast(new SyncSoundMessage { Timestamp = DateTimeOffset.Now, Sound = new Sound { Id = sound.Id } });
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

                server.Broadcast(msg);
            }
        }

        void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServerViewModel.SoundFiles))
            {

            }
        }

        void HostServerTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        async void StartServerTapped(object sender, TappedRoutedEventArgs e)
        {
            if (punshThroughSwitch.IsOn)
            {
                if (!await server.RegisterOnPunchServerAsync(serverTextBox.Text, new HostName(Model.Settings.PunchServerAddress), Model.Settings.PunchServerPort))
                {
                    var startAnywayDialog = new MessageDialog("Failed to register on punch server. Start local direct-ip server anyways?");
                    var yesCommand = new UICommand("Yes");
                    startAnywayDialog.Commands.Add(yesCommand);
                    startAnywayDialog.Commands.Add(new UICommand("No"));
                    if (await startAnywayDialog.ShowAsync() != yesCommand)
                        return;
                }
            }

            if (!await server.Start())
            {
                await new MessageDialog("Failed to start local server.").ShowAsync();
            }

            startServerFlyout.Hide();
        }

        void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                await AddFiles(items.Cast<StorageFile>());
            }
        }

        async void AddNewSound(object sender, TappedRoutedEventArgs e)
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

        async void RemoveSound(object sender, EventArgs e)
        {
            if (Model.SoundFiles.Contains(sender))
            {
                Model.SoundFiles.Remove((Sound)sender);
                await Model.SaveSoundFilesAsync(string.IsNullOrWhiteSpace(serverTextBox.Text) ? "direct" : serverTextBox.Text);
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
    }
}
