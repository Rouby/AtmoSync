using AtmoSync.Shared.Messages;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace AtmoSync.Server
{
    class ClientHandler
    {
        private ClientHandler()
        {

        }

        StreamReader Reader { get; set; }
        StreamWriter Writer { get; set; }

        BlockingCollection<Message> MessageQueue { get; set; } = new BlockingCollection<Message>();

        public static ClientHandler CreateNew(StreamSocket socket)
        {
            return new ClientHandler
            {
                Reader = new StreamReader(socket.InputStream.AsStreamForRead()),
                Writer = new StreamWriter(socket.OutputStream.AsStreamForWrite())
            };
        }

        public void EnqueueMessage(Message msg)
        {
            MessageQueue.Add(msg);
        }

        IEnumerable<Message> TryParseMessage(string json)
        {
            yield return JsonConvert.DeserializeObject<PlaySoundMessage>(json);
            yield return JsonConvert.DeserializeObject<PlaySoundMessage>(json);
            yield return JsonConvert.DeserializeObject<PlaySoundMessage>(json);
        }

        public async Task Run()
        {
            while (true)
            {
                var outMsg = MessageQueue.Take();
                await Writer.WriteLineAsync(JsonConvert.SerializeObject(outMsg));

                var json = await Reader.ReadLineAsync();
                var msg = TryParseMessage(json).DefaultIfEmpty(null).FirstOrDefault(m => m != null);

                if (msg is OkMessage)
                {

                }
                else if (msg is SyncSoundMessage)
                {

                }
                else if (msg is PlaySoundMessage)
                {

                }
                else if (msg is StopSoundMessage)
                {

                }
                else
                {
                    return;
                }
            }
        }
    }
}
