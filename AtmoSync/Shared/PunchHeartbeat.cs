using System;

namespace AtmoSync.Shared
{
    class PunchHeartbeat
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }
}
