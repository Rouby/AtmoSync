using System;

namespace AtmoSync.Shared.Messages
{
    abstract class Message
    {
        public DateTimeOffset Timestamp { get; set; }
    }
}
