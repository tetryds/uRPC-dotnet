using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Threading.Channels;
using uRPC.Core;
using uRPC.Core.ChannelWrapper;
using uRPC.Core.Messages;
using uRPC.Server.Exceptions;
using uRPC.Server.Listening;
using uRPC.Tools;

namespace uRPC.Server;

public class App
{
    readonly AppSettings settings = new();

    uint connectionCount = 0;

    SafeFlag running = new();
    Dictionary<ClientConnection, uint> connections = [];

    public Task? Listener { get; private set; }

    public async Task Start(CancellationToken cancellationToken)
    {
        if (!running.Set())
            throw new Exception("Application can only run once");

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var ct = cts.Token;
        // start listening on ports

        if (settings.ports.Count == 0)
            throw new AppInitializationException("Cannot initialize app while not listening to any ports. Use app.ListenTo(port) to include ports");

        HashSet<Listener> listeners = [];

        var receiveChannel = Channel.CreateUnbounded<(ClientConnection, RawMessage)>();

        var writer = new Writer(receiveChannel.Writer);
        var reader = new Reader(receiveChannel.Reader);

        foreach (var port in settings.ports)
        {
            Listener listener = new(port);
            listeners.Add(listener);
            listener.Connected += c => HookConnection(c, connections, writer, ct);
        }

        foreach (var listener in listeners)
        {
            listener.ListenJob(ct);
        }

        await foreach (var (conn, msg) in reader.ReadAllAsync(ct))
        {
            if (!connections.TryGetValue(conn, out uint id))
                id = 0;

            if (msg.Type == EchoMessage.Type)
            {
                var echoMsg = EchoMessage.Deserialize(msg.Payload);

                var response = new RawMessage
                {
                    Id = msg.Id,
                    Payload = msg.Payload,
                    Status = MessageStatus.Close,
                    Type = msg.Type
                };

                for (int i = 0; i < echoMsg.ReplyCount; i++)
                {
                    var replyEchoMsg = new EchoMessage(i, echoMsg.Data);
                    response.Payload = replyEchoMsg.Serialize();

                    bool isLast = i == echoMsg.ReplyCount - 1;
                    response.Status = isLast ? MessageStatus.Close : MessageStatus.Continue;
                    await conn.SendAsync(response, ct);
                }
            }

        }
    }

    public App ListenTo(int port)
    {
        settings.ports.Add(port);
        return this;
    }

    private class AppSettings()
    {
        public HashSet<int> ports = [];
    }

    private void HookConnection(TcpClient client, Dictionary<ClientConnection, uint> connections, Writer writer, CancellationToken cancellationToken)
    {
        uint id = Interlocked.Increment(ref connectionCount);

        var conn = new ClientConnection(client);
        conn.Disconnected += () => connections.Remove(conn);
        conn.Start(writer, cancellationToken);

        connections.Add(conn, id);
    }
}
