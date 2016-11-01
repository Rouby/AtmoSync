using AtmoSync.Shared.Messages;
using Polenter.Serialization;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;

namespace AtmoSync.Client
{
    class ServerHandler
    {
        private ServerHandler()
        {

        }

        Stream Reader { get; set; }
        Stream Writer { get; set; }
        IClient Client { get; set; }

        SharpSerializer Serializer { get; set; } = new SharpSerializer();

        BlockingCollection<Tuple<Message, Action>> MessageQueue { get; set; } = new BlockingCollection<Tuple<Message, Action>>();

        public static ServerHandler CreateNew(StreamSocket socket, IClient client)
        {
            return new ServerHandler
            {
                Reader = socket.InputStream.AsStreamForRead(),
                Writer = socket.OutputStream.AsStreamForWrite(),
                Client = client
            };
        }
        

        public async Task Run()
        {
            while (true)
            {
                var msg = Serializer.Deserialize(Reader);

                if (msg is OkMessage)
                {
                }
                else if(msg is SyncSoundMessage)
                {
                    var sound = ((SyncSoundMessage)msg).Sound;
                    if (!await Client.SoundExistsAsync(sound.Id))
                    {
                        Serializer.Serialize(new RequestFileMessage { Timestamp = DateTimeOffset.Now, SoundId = sound.Id }, Writer);
                        await Writer.FlushAsync();

                        var sizbuf = new byte[sizeof(long)];
                        await Reader.ReadAsync(sizbuf, 0, sizeof(long));

                        try
                        {
                            var file = await KnownFolders.MusicLibrary.CreateFileAsync(new Uri(sound.File).Segments.Last());
                            sound.File = file.Path;
                            var buffer = new byte[BitConverter.ToInt64(sizbuf, 0)];
                            await Reader.ReadAsync(buffer, 0, (int)BitConverter.ToInt64(sizbuf, 0));
                            await FileIO.WriteBufferAsync(file, buffer.AsBuffer());
                        }
                        catch(Exception e)
                        {
                            sound.File = Path.Combine(KnownFolders.MusicLibrary.Path, new Uri(sound.File).Segments.Last());
                        }

                        Client.AddSound(sound);
                    }
                    Client.SyncSound(sound.Id, sound);
                    Serializer.Serialize(new OkMessage { Timestamp = DateTimeOffset.Now }, Writer);
                    await Writer.FlushAsync();
                }
                else
                {
                    // TODO error handling
                }
            }
        }
    }
}
