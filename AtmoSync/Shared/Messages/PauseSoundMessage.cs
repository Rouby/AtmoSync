using System;

namespace AtmoSync.Shared.Messages
{
    class PauseSoundMessage : Message
    {
        public Guid SoundId { get; set; }
    }
}
