using System;

namespace AtmoSync.Shared
{
    class Sound
    {
        public Guid Id { get; set; }
        public string File { get; set; }
        public string ServerName { get; set; }
        public string ClientName { get; set; }
        public double Volume { get; set; }
        public bool Sync { get; set; }
        public bool Loop { get; set; }
    }
}
