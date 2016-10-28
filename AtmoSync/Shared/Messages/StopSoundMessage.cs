using System;

namespace AtmoSync.Shared.Messages
{
    class StopSoundMessage : Message
    {
        public Guid SoundId { get; set; }
    }
}
