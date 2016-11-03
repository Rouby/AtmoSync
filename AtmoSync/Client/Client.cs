using AtmoSync.Shared;
using AtmoSync.Shared.Messages;
using Newtonsoft.Json;
using Polenter.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace AtmoSync.Client
{
    class SoundStatusEventArgs : EventArgs
    {
        public Guid Id { get; set; }
        public Status Status { get; set; }
        public TimeSpan Diff { get; set; }
    }

    class SoundReceivedEventArgs : EventArgs
    {
        public Sound Sound { get; set; }
        public byte[] Buffer { get; set; }
    }

    class Client
    {
        static readonly SharpSerializer serializer = new SharpSerializer();

        public IClient Interface { get; set; }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<Sound> SoundSyncronized;
        public event EventHandler<SoundStatusEventArgs> SoundStatusChanged;
        public event EventHandler<SoundReceivedEventArgs> SoundFileReceived;

        Stream reader;
        Stream writer;

        CancellationTokenSource cts = new CancellationTokenSource();

        public async Task<bool> Connect(string alias, HostName punchServer, string punchServiceName)
        {
            try
            {
                HostName resolvedHost;
                string serviceName;
                using (var punchSocket = new StreamSocket())
                {
                    await punchSocket.ConnectAsync(punchServer, punchServiceName);

                    using (var punchWriter = new StreamWriter(punchSocket.OutputStream.AsStreamForWrite()))
                    using (var punchReader = new StreamReader(punchSocket.InputStream.AsStreamForRead()))
                    {
                        await punchWriter.WriteLineAsync(JsonConvert.SerializeObject(new PunchRequest { ConnectTo = alias }));
                        await punchWriter.FlushAsync();

                        var response = JsonConvert.DeserializeObject<PunchResponse>(await punchReader.ReadLineAsync());

                        if (!response.Valid)
                            throw new Exception(response.Message ?? "Could not connect to remote server.");

                        resolvedHost = new HostName(response.ServerAddress);
                        serviceName = response.ServerPort;
                    }
                }

                return await Connect(resolvedHost, serviceName);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Connect(HostName host, string serviceName)
        {
            try
            {
                var socket = new StreamSocket();
                await socket.ConnectAsync(host, serviceName);

                reader = socket.InputStream.AsStreamForRead();
                writer = socket.OutputStream.AsStreamForWrite();

                Connected?.Invoke(this, EventArgs.Empty);

                var ignore = Run(cts.Token);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void Disconnect()
        {
            cts.Cancel();
        }


        async Task Run(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var msg = serializer.Deserialize(reader) as Message;

                    if (msg is SyncSoundMessage)
                    {
                        var sound = ((SyncSoundMessage)msg).Sound;
                        if (!await Interface.SoundExistsAsync(sound.Id, sound.File))
                        {
                            await RequestSoundFile(sound);
                        }
                        SoundSyncronized?.Invoke(this, sound);

                        await SendOk();
                    }
                    else if (msg is PlaySoundMessage
                        || msg is PauseSoundMessage
                        || msg is StopSoundMessage)
                    {
                        var id = (msg as PlaySoundMessage)?.SoundId
                            ?? (msg as PauseSoundMessage)?.SoundId
                            ?? (msg as StopSoundMessage).SoundId;

                        var status = (msg is PlaySoundMessage) ? Status.Playing
                            : (msg is PauseSoundMessage) ? Status.Paused
                            : Status.Stopped;

                        SoundStatusChanged?.Invoke(this, new SoundStatusEventArgs { Id = id, Status = status, Diff = DateTimeOffset.Now - msg.Timestamp });

                        await SendOk();
                    }
                    else
                    {
                        serializer.Serialize(new UnknownMessage { Timestamp = DateTimeOffset.Now }, writer);
                        await writer.FlushAsync();
                    }
                }
            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        async Task SendOk()
        {
            serializer.Serialize(new OkMessage { Timestamp = DateTimeOffset.Now }, writer);
            await writer.FlushAsync();
        }

        async Task<bool> RequestSoundFile(Sound sound)
        {
            serializer.Serialize(new RequestFileMessage { Timestamp = DateTimeOffset.Now, SoundId = sound.Id }, writer);
            await writer.FlushAsync();

            var sizbuf = new byte[sizeof(long)];
            await reader.ReadAsync(sizbuf, 0, sizeof(long));
            var count = (int)BitConverter.ToInt64(sizbuf, 0);

            try
            {
                var buffer = new byte[count];
                await reader.ReadAsync(buffer, 0, count);

                SoundFileReceived?.Invoke(this, new SoundReceivedEventArgs { Sound = sound, Buffer = buffer });
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
