using System.Net;
using System.Net.Sockets;
using uRPC.Tools;

namespace uRPC.Server.Listening;

public class Listener(int port)
{
    readonly SafeFlag running = new();

    readonly TcpListener listener = new(IPAddress.Any, port);

    public event Action<TcpClient>? Connected;
    public event Action<ListenException>? Except;
    public event Action? Cancelled;

    public async void ListenJob(CancellationToken cancellationToken)
    {
        //Run only once
        if (!running.Set()) return;

        listener.Start();
        try
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);

                Connected?.Invoke(client);
            }
        }
        catch (OperationCanceledException)
        {
            Cancelled?.Invoke();
        }
        catch (Exception e)
        {
            Except?.Invoke(new ListenException(port, "Error ocurred when listening.", e));
        }
        finally
        {
            listener.Stop();
        }
    }
}
