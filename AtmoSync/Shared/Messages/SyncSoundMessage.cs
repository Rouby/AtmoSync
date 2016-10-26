namespace AtmoSync.Shared.Messages
{
    class SyncSoundMessage : Message
    {
        public Sound Sound { get; set; }
        public int SoundStreams { get; set; }
    }
}
