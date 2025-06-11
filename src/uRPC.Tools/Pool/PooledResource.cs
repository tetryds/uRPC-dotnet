namespace uRPC.Tools.Pool;

public class PooledResource<T>(T value, Func<T, Task> returner) : IAsyncDisposable
{
    public T Value => !disposed.IsSet ? value : throw new ObjectDisposedException(nameof(PooledResource<T>));

    SafeFlag disposed = new();

    public async ValueTask DisposeAsync()
    {
        if (!disposed.Set()) return;

        GC.SuppressFinalize(this);
        await returner(value);
    }
}
