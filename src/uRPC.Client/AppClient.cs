
using uRPC.Core.Messages;
using uRPC.Core;
using uRPC.Core.ChannelWrapper;
using System.IO;
using uRPC.Tools;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace uRPC.Client;

public class AppClient(string hostname, int port)
{
    readonly SafeFlag listening = new();

    private uint sentCount = 0;

    readonly ClientConnection client = new ClientConnection(new TcpClient(hostname, port));

    readonly ConcurrentDictionary<uint, Action<RawMessage>> responseMap = [];

    public async Task<ResponseHandler<TResponse>> SendAsync<TRequest, TResponse>(TRequest message, Action<TResponse>? onReceive = null, CancellationToken cancellationToken = default)
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse>
    {
        uint id = Interlocked.Increment(ref sentCount);

        var raw = new RawMessage
        {
            Id = id,
            Status = MessageStatus.Continue,
            Type = TRequest.Type,
            Payload = message.Serialize()
        };

        var handler = new ResponseHandler<TResponse>(onReceive);
        _ = handler.Wait.ContinueWith(_ => responseMap.TryRemove(id, out var _));

        if (!responseMap.TryAdd(id, handler.AddResponse))
            throw new Exception("Failed to send request, seems like ID mapping is borked");

        await client.SendAsync(raw, cancellationToken);

        return handler;
    }

    public async void Start(CancellationToken cancellationToken)
    {
        // Can only start once
        if (!listening.Set()) return;

        var receiveChannel = Channel.CreateUnbounded<(ClientConnection, RawMessage)>();

        var writer = new Writer(receiveChannel.Writer);
        var reader = new Reader(receiveChannel.Reader);

        client.Start(writer, cancellationToken);

        while (true)
        {
            (_, var msg) = await reader.ReadAsync(cancellationToken);

            if (responseMap.TryGetValue(msg.Id, out var handleResponse))
                handleResponse(msg);
        }
    }
}