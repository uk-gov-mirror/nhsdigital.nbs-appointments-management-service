using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Nhs.Appointments.Core.Caching;

public class InMemoryCacheStore(IMemoryCache memoryCache) : ICacheStore
{
    private readonly ConcurrentDictionary<string, object> _cacheLock = new();
    public Task<CacheStoreResponse<T>> TryGetAsync<T>(string key) 
        => Task.FromResult(memoryCache.TryGetValue(key, out T value) 
            ? CacheStoreResponses.Success(value) : CacheStoreResponses.Fail<T>());
    public Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration) => Task.FromResult(memoryCache.Set(key, value, absoluteExpiration));

    public Task<bool> CanUpdateCacheAsync(string key)
    {
        memoryCache.Get<bool>($"{key}:lock");
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expirationRelativeToNow) => Task.FromResult(memoryCache.Set(key, value, expirationRelativeToNow));
}
