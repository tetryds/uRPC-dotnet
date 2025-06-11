using System.Runtime.CompilerServices;
using System.Threading.Channels;
using uRPC.Core.Messages;

namespace uRPC.Core.ChannelWrapper;

public class Reader(ChannelReader<(ClientConnection, RawMessage)> reader)
{
    public IAsyncEnumerable<(ClientConnection, RawMessage)> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask<(ClientConnection, RawMessage)> ReadAsync(CancellationToken cancellationToken = default)
    {
        return reader.ReadAsync(cancellationToken);
    }
}
