using AtmoSync.Shared.Messages;
using Polenter.Serialization;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;

namespace AtmoSync.Server
{
    class ClientHandler
    {
        private ClientHandler()
        {

        }

        Stream Reader { get; set; }
        Stream Writer { get; set; }
        IServer Server { get; set; }

        SharpSerializer Serializer { get; set; } = new SharpSerializer();

        BlockingCollection<Tuple<Message, Action>> MessageQueue { get; set; } = new BlockingCollection<Tuple<Message, Action>>();

        public static ClientHandler CreateNew(StreamSocket socket, IServer server)
        {
            return new ClientHandler
            {
                Reader = socket.InputStream.AsStreamForRead(),
                Writer = socket.OutputStream.AsStreamForWrite(),
                Server = server
            };
        }

        public void EnqueueMessage(Message msg, Action callback = null)
        {
            MessageQueue.Add(Tuple.Create(msg, callback));
        }

        public async Task Run()
        {
            while (true)
            {
                var outGoing = MessageQueue.Take();
                Serializer.Serialize(outGoing.Item1, Writer);
                await Writer.FlushAsync();

                var msg = Serializer.Deserialize(Reader);

                if (msg is OkMessage)
                {
                    outGoing.Item2?.Invoke();
                }
                else if (msg is RequestFileMessage)
                {
                    IStorageFile file = await Server.GetSoundFileAsync(((RequestFileMessage)msg).SoundId);
                    var stream = await file.OpenStreamForReadAsync();
                    await Writer.WriteAsync(BitConverter.GetBytes(stream.Length), 0, sizeof(long));
                    await Writer.FlushAsync();
                    await stream.CopyToAsync(Writer);
                    await Writer.FlushAsync();

                    msg = Serializer.Deserialize(Reader);

                    if (msg is OkMessage)
                    {
                        outGoing.Item2?.Invoke();
                    }
                    else
                    {
                        // TODO error handling 2
                    }
                }
                else
                {
                    // TODO error handling
                }
            }
        }
    }
}
