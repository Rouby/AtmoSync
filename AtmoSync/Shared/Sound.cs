using Newtonsoft.Json;
using System;

namespace AtmoSync.Shared
{
    enum Status
    {
        Stopped,
        Playing,
        Paused
    }

    class Sound : BindableBase
    {
        Guid _id;
        public Guid Id { get { return _id; } set { SetProperty(ref _id, value); } }

        string _file;
        public string File { get { return _file; } set { SetProperty(ref _file, value); } }

        string _serverName;
        public string ServerName { get { return _serverName; } set { SetProperty(ref _serverName, value); } }

        string _clientName;
        public string ClientName { get { return _clientName; } set { SetProperty(ref _clientName, value); } }

        double _volume;
        public double Volume { get { return _volume; } set { SetProperty(ref _volume, value); } }

        bool _sync;
        public bool Sync { get { return _sync; } set { SetProperty(ref _sync, value); } }

        bool _loop;
        public bool Loop { get { return _loop; } set { SetProperty(ref _loop, value); } }

        Status _status;
        [JsonIgnore]
        public Status Status { get { return _status; } set { SetProperty(ref _status, value); } }

        bool _synced;
        [JsonIgnore]
        public bool IsSynced { get { return _synced; } set { SetProperty(ref _synced, value); } }
    }
}
