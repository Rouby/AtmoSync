using System;

namespace AtmoSync.Shared.Messages
{
    class RequestFileMessage : Message
    {
        public Guid SoundId { get; set; }
    }
}
