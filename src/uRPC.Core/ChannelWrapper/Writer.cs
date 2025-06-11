using System.Threading.Channels;
using uRPC.Core.Messages;

namespace uRPC.Core.ChannelWrapper;

public class Writer(ChannelWriter<(ClientConnection, RawMessage)> writer)
{
    public async Task WriteAsync((ClientConnection, RawMessage msg) value, CancellationToken cancellationToken)
    {
        await writer.WriteAsync(value, cancellationToken);
    }
}
