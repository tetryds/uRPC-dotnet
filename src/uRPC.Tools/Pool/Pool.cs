using System.Collections.Concurrent;

namespace uRPC.Tools.Pool;

public class AsyncPool<T>(Func<Task<T>> generator, Func<T, Task> destructor, int maxPoolSize)
{
    readonly ConcurrentQueue<T> pool = [];

    public async Task<PooledResource<T>> GetAsync()
    {
        if (!pool.TryDequeue(out T? obj))
        {
            obj = await generator();
        }

        return new PooledResource<T>(obj, ReturnAsync);
    }

    private async Task ReturnAsync(T value)
    {
        if (pool.Count < maxPoolSize)
            pool.Enqueue(value);
        else
            await destructor(value);
    }
}
