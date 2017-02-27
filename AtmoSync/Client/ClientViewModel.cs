using AtmoSync.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;

namespace AtmoSync.Client
{
    class ClientViewModel : BindableBase
    {
        ObservableCollection<Sound> _soundFiles = new ObservableCollection<Sound>();
        public IList<Sound> SoundFiles { get { return _soundFiles; } set { _soundFiles.Clear(); foreach (var item in value) _soundFiles.Add(item); OnPropertyChanged(nameof(SoundFiles)); } }

        public IDictionary<Guid, double> LocalSoundVolumes { get; set; } = new Dictionary<Guid, double>();

        bool _connected;
        public bool Connected { get { return _connected; } set { SetProperty(ref _connected, value); } }

        Settings _settings;
        public Settings Settings { get { return _settings; } set { SetProperty(ref _settings, value); } }

        public ClientViewModel()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            LoadSettingsAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        async Task LoadSettingsAsync()
        {
            var localData = ApplicationData.Current.LocalFolder;

            var settingsFile = await localData.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);
            var settings = JsonConvert.DeserializeObject<Settings>(await FileIO.ReadTextAsync(settingsFile));

            if (settings == null)
            {
                await FileIO.WriteTextAsync(settingsFile, JsonConvert.SerializeObject(settings = new Settings
                {
                    PunchServerAddress = "127.0.0.1",
                    PunchServerPort = "34512"
                }));
            }

            Settings = settings;
        }


        public async Task LoadSoundFilesAsync(string server)
        {
            var uriServer = Uri.EscapeUriString(server);
            var localData = ApplicationData.Current.LocalFolder;


            var soundFilesSettings = await localData.CreateFileAsync($"{uriServer}-soundFiles.json", CreationCollisionOption.OpenIfExists);
            var soundFiles = JsonConvert.DeserializeObject<IList<Sound>>(await FileIO.ReadTextAsync(soundFilesSettings));
            if (soundFiles == null)
            {
                await FileIO.WriteTextAsync(soundFilesSettings, JsonConvert.SerializeObject(SoundFiles));
            }
            else
            {
                SoundFiles = soundFiles;
            }


            var localSoundVolumesSettings = await localData.CreateFileAsync($"{uriServer}-soundVolumes.json", CreationCollisionOption.OpenIfExists);
            var localSoundVolumes = JsonConvert.DeserializeObject<IDictionary<Guid, double>>(await FileIO.ReadTextAsync(localSoundVolumesSettings));
            if (localSoundVolumes == null)
            {
                await FileIO.WriteTextAsync(localSoundVolumesSettings, JsonConvert.SerializeObject(LocalSoundVolumes));
            }
            else
            {
                LocalSoundVolumes = localSoundVolumes;
            }
        }

        public async Task SaveSoundFilesAsync(string server)
        {
            var uriServer = Uri.EscapeUriString(server);
            var localData = ApplicationData.Current.LocalFolder;

            var soundFilesSettings = await localData.CreateFileAsync($"{uriServer}-soundFiles.json", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(soundFilesSettings, JsonConvert.SerializeObject(SoundFiles));


            var localSoundVolumesSettings = await localData.CreateFileAsync($"{uriServer}-soundVolumes.json", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(localSoundVolumesSettings, JsonConvert.SerializeObject(LocalSoundVolumes));
        }
    }
}
