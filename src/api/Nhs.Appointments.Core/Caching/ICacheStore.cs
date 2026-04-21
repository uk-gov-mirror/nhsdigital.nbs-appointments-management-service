namespace Nhs.Appointments.Core.Caching;

public interface ICacheStore
{
    Task<CacheStoreResponse<T>> TryGetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration);
    Task<bool> CanUpdateCacheAsync(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expirationRelativeToNow);
}
