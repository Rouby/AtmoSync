using AtmoSync.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;

namespace AtmoSync.Server
{
    class ServerViewModel : BindableBase
    {
        ObservableCollection<Sound> _soundFiles = new ObservableCollection<Sound>();
        public IList<Sound> SoundFiles { get { return _soundFiles; } set { SetProperty(ref _soundFiles, new ObservableCollection<Sound>(value)); } }


        bool _listening;
        public bool Listening { get { return _listening; } set { SetProperty(ref _listening, value); } }

        Settings _settings;
        public Settings Settings { get { return _settings; } set { SetProperty(ref _settings, value); } }

        public ServerViewModel()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            LoadSettingsAsync();
            SoundFiles = new List<Sound> {
                new Sound
                {
                    Id = Guid.NewGuid(),
                    File = "C:/asd/asd/asd",
                    ServerName = "Orc-Growl",
                    ClientName = "Some Sound",
                    Loop = false,
                    Sync = false,
                    Volume = 1.0
                }
            };
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


        public async Task LoadSoundFilesAsync(string alias)
        {
            var uriServer = Uri.EscapeUriString(alias);
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
        }

        public async Task SaveSoundFilesAsync(string alias)
        {
            var uriServer = Uri.EscapeUriString(alias);
            var localData = ApplicationData.Current.LocalFolder;

            var soundFilesSettings = await localData.CreateFileAsync($"{uriServer}-soundFiles.json", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(soundFilesSettings, JsonConvert.SerializeObject(SoundFiles));
        }
    }
}
