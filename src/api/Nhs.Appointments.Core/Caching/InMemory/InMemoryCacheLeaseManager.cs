namespace Nhs.Appointments.Core.Caching;

public class InMemoryCacheLeaseManager : ICacheLeaseManager
{
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public Task<ICacheLeaseContext> Acquire(string key)
    {
        SemaphoreSlim mutex;

        lock (_locks)
        {
            if (!_locks.ContainsKey(key))
            {
                _locks.Add(key, new SemaphoreSlim(1,1));
            }
            mutex = _locks[key];
        }

        return Task.FromResult<ICacheLeaseContext>(new CacheLeaseContext(key, () => Task.FromResult(mutex.Release())));
    }
}
