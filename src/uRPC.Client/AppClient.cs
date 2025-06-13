
using uRPC.Core.Messages;
using uRPC.Core;
using uRPC.Core.ChannelWrapper;
using System.IO;
using uRPC.Tools;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace uRPC.Client;

public class AppClient : IMessageHandler
{
    private uint sentCount = 0;

    readonly ClientConnection client;

    readonly ConcurrentDictionary<uint, Action<RawMessage>> responseMap = [];

    public AppClient(string hostname, int port)
    {
        client = new ClientConnection(new TcpClient(hostname, port), this);
    }

    public async Task<ResponseHandler<TRequest, TResponse>> SendAsync<TRequest, TResponse>(TRequest message, Action<TResponse>? onReceive = null, CancellationToken cancellationToken = default)
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

        var handler = new ResponseHandler<TRequest, TResponse>(message, onReceive);
        _ = handler.Wait.ContinueWith(_ => responseMap.TryRemove(id, out var _));

        if (!responseMap.TryAdd(id, handler.AddResponse))
            throw new Exception("Failed to send request, seems like ID mapping is borked");

        await client.SendAsync(raw, cancellationToken);

        return handler;
    }

    public AppClient Start(CancellationToken cancellationToken)
    {
        client.Start(cancellationToken);
        return this;
    }

    public Task HandleMessageAsync(ClientConnection conn, RawMessage message, CancellationToken ct)
    {
        if (responseMap.TryGetValue(message.Id, out var handleResponse))
            handleResponse(message);
        return Task.CompletedTask;
    }
}