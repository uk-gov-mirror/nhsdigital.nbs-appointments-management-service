using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Nhs.Appointments.Core.Caching.Redis;

public class RedisCacheStore(IRedisConnection redisConnection, ILogger<RedisCacheStore> logger) : ICacheStore
{
    public async Task<CacheStoreResponse<T>> TryGetAsync<T>(string key)
    {
        try
        {
            var connection = await redisConnection.Get();
            var database = connection.GetDatabase();
            var value = JsonConvert.DeserializeObject<T>(await database.StringGetAsync(key));
            return CacheStoreResponses.Success(value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve cache of {Type} with key {Key}", typeof(T), key);
            return CacheStoreResponses.Fail<T>();
        }
    }

    public async Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration)
    {
        try
        {
            var connection = await redisConnection.Get();
            var database = connection.GetDatabase();
            await database.StringSetAsync(key, JsonConvert.SerializeObject(value), absoluteExpiration.UtcDateTime);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache of {Type} with key {Key}", typeof(T), key);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expirationRelativeToNow) 
    {
        try
        {
            var connection = await redisConnection.Get();
            var database = connection.GetDatabase();
            await database.StringSetAsync(key, JsonConvert.SerializeObject(value), expirationRelativeToNow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache of {Type} with key {Key}", typeof(T), key);
        }
    }
}
