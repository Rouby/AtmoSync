using AtmoSync.Shared;
using AtmoSync.Shared.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace AtmoSync.Server
{
    class Server
    {
        public string Alias { get; private set; } = "direct";
        public string ServiceName { get; set; } = "56779";
        public IServer Interface { get; set; }
        public int ConnectedClients { get { return clients.Count; } }

        public event EventHandler<ClientHandler> ClientConnected;
        public event EventHandler ClientDisconnected;
        public event EventHandler Startup;
        public event EventHandler Teardown;
        public event EventHandler PunchSetup;
        public event EventHandler HeartbeatFailed;

        StreamSocket punchSocket;
        StreamReader punchReader;
        StreamWriter punchWriter;

        StreamSocketListener listener;
        CancellationTokenSource cts = new CancellationTokenSource();

        ConcurrentDictionary<Guid, ClientHandler> clients;

        public async Task<bool> RegisterOnPunchServerAsync(string alias, HostName punchServer, string punchServiceName)
        {
            try
            {
                punchSocket = new StreamSocket();
                await punchSocket.ConnectAsync(punchServer, punchServiceName);

                punchReader = new StreamReader(punchSocket.InputStream.AsStreamForRead());
                punchWriter = new StreamWriter(punchSocket.OutputStream.AsStreamForWrite());

                await punchWriter.WriteLineAsync(JsonConvert.SerializeObject(new PunchRequest { ServerAlias = alias }));
                await punchWriter.FlushAsync();

                var response = JsonConvert.DeserializeObject<PunchResponse>(await punchReader.ReadLineAsync());

                if (!response.Valid)
                    throw new Exception(response.Message ?? "Could not establish server alias.");

                Alias = alias;

                var ignored = PunchServerHeartbeat(cts.Token);

                PunchSetup?.Invoke(this, new EventArgs { });
            }
            catch
            {
                punchReader?.Dispose();
                punchWriter?.Dispose();
                punchSocket?.Dispose();
                punchReader = null;
                punchWriter = null;
                punchSocket = null;

                return false;
            }

            return true;
        }

        async Task PunchServerHeartbeat(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10000);

                await punchWriter.WriteLineAsync(JsonConvert.SerializeObject(new PunchHeartbeat()));
                await punchWriter.FlushAsync();

                var response = JsonConvert.DeserializeObject<PunchResponse>(await punchReader.ReadLineAsync());

                if (!response.Valid)
                {
                    HeartbeatFailed?.Invoke(this, new EventArgs { });
                    break;
                }
            }
        }

        public async Task<bool> Start()
        {
            try
            {
                listener = new StreamSocketListener();
                listener.Control.QualityOfService = SocketQualityOfService.Normal;
                listener.ConnectionReceived += ConnectionReceived;

                clients = new ConcurrentDictionary<Guid, ClientHandler>();

                await listener.BindServiceNameAsync(ServiceName);

                Startup?.Invoke(this, new EventArgs { });
            }
            catch
            {
                Stop();
                return false;
            }

            return true;
        }

        public void Stop()
        {
            cts.Cancel();
            listener?.Dispose();
            listener = null;

            Teardown?.Invoke(this, new EventArgs { });
        }

        async void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var id = Guid.NewGuid();
            var client = ClientHandler.CreateNew(args.Socket, Interface, cts.Token);
            clients.TryAdd(id, client);

            ClientConnected?.Invoke(this, client);

            await client.Run();

            ClientDisconnected?.Invoke(this, new EventArgs { });

            clients.TryRemove(id, out client);
        }

        public void Broadcast(Message msg, Action success = null, Action failure = null)
        {
            foreach (var client in clients.Values)
                client.EnqueueMessage(msg, success, failure);
        }
    }
}
