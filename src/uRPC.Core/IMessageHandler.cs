using uRPC.Core.Messages;

namespace uRPC.Core;

public interface IMessageHandler
{
    Task HandleMessageAsync(ClientConnection conn, RawMessage message, CancellationToken ct);
}
