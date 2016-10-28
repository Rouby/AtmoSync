using System;

namespace PunchServer
{
    class PunchHeartbeat
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }
}
