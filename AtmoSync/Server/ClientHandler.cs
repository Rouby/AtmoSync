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
    class QueuedMessage
    {
        public readonly Message Message;
        public readonly Action Success;
        public readonly Action Failure;

        public QueuedMessage(Message message, Action success, Action failure)
        {
            Message = message;
            Success = success;
            Failure = failure;
        }
    }

    class ClientHandler
    {
        static readonly SharpSerializer serializer = new SharpSerializer();

        public static ClientHandler CreateNew(StreamSocket socket, IServer server, CancellationToken token)
        {
            return new ClientHandler
            {
                reader = socket.InputStream.AsStreamForRead(),
                writer = socket.OutputStream.AsStreamForWrite(),
                server = server,
                token = token
            };
        }

        private ClientHandler()
        {

        }

        Stream reader;
        Stream writer;
        IServer server;

        CancellationToken token;

        readonly BlockingCollection<QueuedMessage> messageQueue = new BlockingCollection<QueuedMessage>();

        public void EnqueueMessage(Message msg, Action success = null, Action failure = null)
        {
            messageQueue.Add(new QueuedMessage(msg, success, failure));
        }

        public async Task Run()
        {
            while (!token.IsCancellationRequested)
            {
                var outGoing = messageQueue.Take();
                serializer.Serialize(outGoing.Message, writer);
                await writer.FlushAsync();

                var msg = serializer.Deserialize(reader) as Message;

                if (msg is OkMessage)
                {
                    outGoing.Success?.Invoke();
                }
                else if (msg is RequestFileMessage)
                {
                    IStorageFile file = await server.GetSoundFileAsync(((RequestFileMessage)msg).SoundId);
                    await SendFileAsync(file);

                    msg = serializer.Deserialize(reader) as Message;

                    if (msg is OkMessage)
                    {
                        outGoing.Success?.Invoke();
                    }
                    else
                    {
                        outGoing.Failure?.Invoke();
                    }
                }
                else
                {
                    outGoing.Failure?.Invoke();
                }
            }
        }

        async Task SendFileAsync(IStorageFile file)
        {
            var fileStream = await file.OpenStreamForReadAsync();

            await writer.WriteAsync(BitConverter.GetBytes(fileStream.Length), 0, sizeof(long));
            await writer.FlushAsync();

            await fileStream.CopyToAsync(writer);
            await writer.FlushAsync();
        }
    }
}
