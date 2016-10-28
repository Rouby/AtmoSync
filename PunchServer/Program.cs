using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

namespace PunchServer
{
    public class Mapping
    {
        public DateTimeOffset LastHeartbeat { get; set; }
        public EndPoint EndPoint { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Run().Wait();
        }

        bool Running { get; set; }
        ConcurrentDictionary<string, Mapping> Mappings { get; set; } = new ConcurrentDictionary<string, Mapping>();

        async Task Run()
        {
            try
            {
                var server = new TcpListener(new IPEndPoint(IPAddress.Any, 34512));

                server.Start();
                Running = true;
                Console.WriteLine($"Server listening on port {server.LocalEndpoint.ToString().Split(':')[1]}.");

                var cts = new CancellationTokenSource();
                FilterInactiveMappingsAsync(cts.Token);

                while (Running)
                {
                    var client = await server.AcceptTcpClientAsync();
                    HandleClientAsync(client);
                }

                cts.Cancel();

                server.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        async Task FilterInactiveMappingsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Mapping removed;
                foreach (var kv in Mappings.Where(kv => (DateTimeOffset.Now - kv.Value.LastHeartbeat).TotalSeconds > 20).ToList())
                    if (Mappings.TryRemove(kv.Key, out removed))
                        Console.WriteLine($"removed inactive mapping {kv.Key} <-> {removed.EndPoint}");

                await Task.Delay(1000);
            }
        }

        async Task HandleClientAsync(TcpClient tcp)
        {
            Console.WriteLine("new client...");

            using (var reader = new StreamReader(tcp.GetStream()))
            using (var writer = new StreamWriter(tcp.GetStream()))
            {
                var request = JsonConvert.DeserializeObject<PunchRequest>(await reader.ReadLineAsync());
                Mapping serverMap = null;

                if (!string.IsNullOrEmpty(request.ServerAlias))
                {
                    Mappings.TryAdd(request.ServerAlias, serverMap = new Mapping { EndPoint = tcp.Client.RemoteEndPoint, LastHeartbeat = DateTimeOffset.Now });

                    Console.WriteLine($"registered server alias as {request.ServerAlias} <-> {serverMap.EndPoint}.");
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchResponse
                    {
                        Valid = true
                    }));
                    await writer.FlushAsync();
                }
                else
                {
                    Mapping mapping;
                    if (Mappings.TryGetValue(request.ConnectTo, out mapping))
                    {
                        var splitEnd = mapping.EndPoint.ToString().Split(':');
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchResponse
                        {
                            Valid = true,
                            ServerAddress = splitEnd[0],
                            ServerPort = splitEnd[1]
                        }));
                        await writer.FlushAsync();
                    }
                    else
                    {
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchResponse
                        {
                            Valid = false,
                            Message = "Server address not available."
                        }));
                        await writer.FlushAsync();
                    }
                }

                while (true)
                {
                    var heartbeat = JsonConvert.DeserializeObject<PunchHeartbeat>(await reader.ReadLineAsync());

                    Console.WriteLine($"HEARTBEAT {heartbeat?.Timestamp}");

                    if (heartbeat == null)
                        break;

                    if (serverMap != null)
                        serverMap.LastHeartbeat = DateTimeOffset.Now;

                    await writer.WriteLineAsync(JsonConvert.SerializeObject(new PunchHeartbeat()));
                    await writer.FlushAsync();
                }
            }

            Console.WriteLine("...client disconnected");
        }
    }
}
