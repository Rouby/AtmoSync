using AtmoSync.Shared;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking;
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

        Client client;

        public ClientPage()
        {
            InitializeComponent();
            DataContext = Model = new ClientViewModel();

            client = new Client { Interface = this };
            client.Connected += ClientConnected;
            client.Disconnected += ClientDisconnected;
            client.SoundSyncronized += ClientSoundSyncronized;
            client.SoundFileReceived += ClientSoundFileReceived;
            client.SoundStatusChanged += ClientSoundStatusChanged;
        }

        void ClientConnected(object sender, EventArgs e)
        {
            Model.Connected = true;
        }

        void ClientDisconnected(object sender, EventArgs e)
        {
            Model.Connected = false;
        }

        async void ClientSoundFileReceived(object sender, SoundReceivedEventArgs e)
        {
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
            {
                var sound = Model.SoundFiles.FirstOrDefault(s => s.Id == e.Sound.Id);
                if (sound == null)
                {
                    Model.SoundFiles.Add(e.Sound);
                    sound = e.Sound;
                }

                var file = await KnownFolders.MusicLibrary.CreateFileAsync($"{e.Sound.ClientName}.{Path.GetExtension(e.Sound.File)}", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, e.Buffer);

                sound.File = file.Path;
            });
        }

        void ClientSoundSyncronized(object sender, Sound eSound)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var sound = Model.SoundFiles.FirstOrDefault(s => s.Id == eSound.Id);
                if (sound == null)
                {
                    Model.SoundFiles.Add(eSound);
                    sound = eSound;
                }

                sound.ClientName = eSound.ClientName;
                sound.Loop = eSound.Loop;
                sound.Volume = eSound.Volume;
            });
        }

        void ClientSoundStatusChanged(object sender, SoundStatusEventArgs e)
        {
            var sound = Model.SoundFiles.FirstOrDefault(s => s.Id == e.Id);
            if (sound != null)
            {
                sound.Status = e.Status;
            }
        }

        void ConnectServerTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        async void InitConnectionTapped(object sender, TappedRoutedEventArgs e)
        {
            IPAddress address;
            var hostName = serverTextBox.Text.Contains(":")
                ? serverTextBox.Text.Split(':')[0]
                : serverTextBox.Text;
            var serviceName = serverTextBox.Text.Contains(":")
                ? serverTextBox.Text.Split(':')[1]
                : "56779";
            if (IPAddress.TryParse(hostName, out address))
            {
                if (!await client.Connect(new HostName(hostName), serviceName))
                {
                    await new MessageDialog("Failed to connect to server.").ShowAsync();
                }
            }
            else
            {
                if (!await client.Connect(hostName, new HostName(Model.Settings.PunchServerAddress), Model.Settings.PunchServerPort))
                {
                    await new MessageDialog("Failed to connect to punch server.").ShowAsync();
                }
            }
            connectServerFlyout.Hide();
        }

        async Task<bool> IClient.SoundExistsAsync(Guid id, string path)
        {
            try
            {
                var sound = Model.SoundFiles.FirstOrDefault(s => s.Id.Equals(id));
                if (sound != null)
                {
                    await StorageFile.GetFileFromPathAsync(sound.File);
                    return true;
                }

                await KnownFolders.MusicLibrary.GetFileAsync(Path.GetFileName(path));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
