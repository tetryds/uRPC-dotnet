using System.Collections.Concurrent;
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

    SafeFlag running = new();

    public Task? Listener { get; private set; }

    public IMessageHandler Handler { get; private set; } = new MessageHandler();

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

        foreach (var port in settings.ports)
        {
            Listener listener = new(port);
            listeners.Add(listener);
            listener.Connected += c => HookConnection(c, ct);
        }

        foreach (var listener in listeners)
        {
            listener.ListenJob(ct);
        }

        await Task.Delay(TimeSpan.MaxValue, cancellationToken);
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

    private void HookConnection(TcpClient client, CancellationToken cancellationToken)
    {
        new ClientConnection(client, Handler).Start(cancellationToken);
    }
}
