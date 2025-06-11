using System.Net.Sockets;
using System.Threading.Channels;
using uRPC.Core.ChannelWrapper;
using uRPC.Core.Messages;
using uRPC.Tools;

namespace uRPC.Core;

public class ClientConnection(TcpClient client)
{
    public event Action<Exception>? Except;
    public event Action? Cancelled;
    public event Action? Disconnected;

    public event Func<RawMessage, Task>? ReceivedAsync;

    readonly SafeFlag listening = new();
    readonly NetworkStream stream = client.GetStream();

    public async Task SendAsync(RawMessage message, CancellationToken cancellationToken)
    {
        await message.WriteBytesAsync(stream, cancellationToken);
    }

    public async void Start(Writer writer, CancellationToken cancellationToken)
    {
        // Can only start once
        if (!listening.Set()) return;

        try
        {
            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                var msg = await RawMessage.ReadNewMessageAsync(stream, cancellationToken);
                await writer.WriteAsync((this, msg), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Cancelled?.Invoke();
        }
        catch (Exception e)
        {
            Except?.Invoke(e);
        }
        finally
        {
            client.Close();
            Disconnected?.Invoke();
            client.Dispose();
        }
    }
}
