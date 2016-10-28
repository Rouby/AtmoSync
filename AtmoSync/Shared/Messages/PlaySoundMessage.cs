using System;

namespace AtmoSync.Shared.Messages
{
    class PlaySoundMessage : Message
    {
        public Guid SoundId { get; set; }
    }
}
