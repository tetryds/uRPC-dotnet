using System.Net.Sockets;
using System.Threading.Channels;
using uRPC.Core.ChannelWrapper;
using uRPC.Core.Messages;
using uRPC.Tools;

namespace uRPC.Core;

public class ClientConnection(TcpClient client, IMessageHandler handler)
{
    public event Action<Exception>? Except;
    public event Action? Cancelled;
    public event Action? Disconnected;

    public event Func<RawMessage, Task>? ReceivedAsync;

    readonly SafeFlag listening = new();
    readonly NetworkStream stream = client.GetStream();

    readonly SemaphoreSlim writerLock = new SemaphoreSlim(1, 1);

    public async Task SendAsync(RawMessage message, CancellationToken cancellationToken)
    {
        //if (!listening.IsSet)
        //    throw new Exception("Cannot send message, client is not started. Please call 'Start()' before sending messages.");
        await writerLock.WaitAsync(cancellationToken);
        await message.WriteBytesAsync(stream, cancellationToken);
        writerLock.Release();
    }

    public async void Start(CancellationToken cancellationToken)
    {
        // Can only start once
        if (!listening.Set()) return;

        var channel = Channel.CreateBounded<RawMessage>(1024);

        var writer = channel.Writer;

        HandleMessages(channel.Reader, cancellationToken);

        try
        {
            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                var msg = await RawMessage.ReadNewMessageAsync(stream, cancellationToken);
                await writer.WriteAsync(msg, cancellationToken);
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

    private async void HandleMessages(ChannelReader<RawMessage> reader, CancellationToken cancellationToken)
    {

        try
        {
            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                var msg = await reader.ReadAsync(cancellationToken);
                await handler.HandleMessageAsync(this, msg, cancellationToken);
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
