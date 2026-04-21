using System.Collections.Concurrent;

namespace Nhs.Appointments.Core.Caching;

public class InMemoryCacheLease : ICacheLease
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

    public async Task AquireLease(string key)
    {
        var cacheLock =
            CacheLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        
        await cacheLock.WaitAsync();
    }

    public Task ReleaseLease(string key)
    {
        if (CacheLocks.TryGetValue(key, out var cacheLock))
        {
            cacheLock.Release();
        }

        return Task.CompletedTask;
    }
}
