using Newtonsoft.Json;
using System;
using System.Threading;

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
        public string ClientName { get { return string.IsNullOrWhiteSpace(_clientName) ? ServerName : _clientName; } set { SetProperty(ref _clientName, value); } }

        double _volume;
        public double Volume { get { return _volume; } set { SetProperty(ref _volume, value); } }

        bool _sync;
        public bool Sync { get { return _sync; } set { SetProperty(ref _sync, value); OnPropertyChanged(nameof(IsSyncing)); OnPropertyChanged(nameof(IsSynced)); } }

        bool _loop;
        public bool Loop { get { return _loop; } set { SetProperty(ref _loop, value); } }

        bool _invalid;
        [JsonIgnore]
        public bool Invalid { get { return _invalid; } set { SetProperty(ref _invalid, value); } }

        string _invalidMessage;
        [JsonIgnore]
        public string InvalidMessage { get { return _invalidMessage; } set { SetProperty(ref _invalidMessage, value); } }

        Status _status;
        [JsonIgnore]
        public Status Status { get { return _status; } set { SetProperty(ref _status, value); } }

        bool _synced = true;
        [JsonIgnore]
        public bool IsSynced { get { return Sync && _synced; } set { SetProperty(ref _synced, value); OnPropertyChanged(nameof(IsSyncing)); } }

        int _syncsOutstanding;
        [JsonIgnore]
        public int SyncsOutstanding { get { return _syncsOutstanding; } set { SetProperty(ref _syncsOutstanding, value); } }

        [JsonIgnore]
        public bool IsSyncing { get { return Sync && !IsSynced; } }


        public void IncrementSyncsOutstanding()
        {
            if (Interlocked.Increment(ref _syncsOutstanding) > 0)
                IsSynced = false;
        }

        public void DecrementSyncsOutstanding()
        {
            if (Interlocked.Decrement(ref _syncsOutstanding) <= 0)
                IsSynced = true;
        }
    }
}
