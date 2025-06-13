using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using uRPC.Core;
using uRPC.Core.Messages;
using uRPC.Tools;

namespace uRPC.Server;

public class MessageHandler : IMessageHandler
{
    delegate Task Handle(ClientConnection conn, RawMessage message, CancellationToken ct);

    readonly Dictionary<uint, Handle> HandlerMap;

    public MessageHandler()
    {
        HandlerMap = new Dictionary<uint, Handle>
        {
            [EchoMessage.Type] = HandleEchoMessage
        };
    }

    public async Task HandleMessageAsync(ClientConnection conn, RawMessage message, CancellationToken ct)
    {
        if (!HandlerMap.TryGetValue(message.Type, out var handle))
            throw new InvalidOperationException($"Unrecognized message type {message.Type}");

        await handle(conn, message, ct);
    }

    private async Task HandleEchoMessage(ClientConnection conn, RawMessage message, CancellationToken ct)
    {
        var echoMsg = EchoMessage.Deserialize(message.Payload);

        var response = new RawMessage
        {
            Id = message.Id,
            Payload = message.Payload,
            Status = MessageStatus.Close,
            Type = message.Type
        };

        for (int i = 0; i < echoMsg.ReplyCount; i++)
        {
            bool isLast = i == echoMsg.ReplyCount - 1;
            response.Status = isLast ? MessageStatus.Close : MessageStatus.Continue;
            await conn.SendAsync(response, ct);
        }
    }
}
