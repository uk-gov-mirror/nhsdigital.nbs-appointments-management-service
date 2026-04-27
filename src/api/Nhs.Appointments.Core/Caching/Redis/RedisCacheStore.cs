using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Nhs.Appointments.Core.Caching.Redis;

public class RedisCacheStore(ConfigurationOptions connectionOptions, ILogger<RedisCacheStore> logger) : ICacheStore, IAsyncDisposable
{
    private ConnectionMultiplexer _connectionMultiplexer;

    public async Task<CacheStoreResponse<T>> TryGetAsync<T>(string key)
    {
        try
        {
            await OpenConnection();
            var database = _connectionMultiplexer.GetDatabase();
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
            await OpenConnection();
            var database = _connectionMultiplexer.GetDatabase();
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
            await OpenConnection();
            var database = _connectionMultiplexer.GetDatabase();
            await database.StringSetAsync(key, JsonConvert.SerializeObject(value), expirationRelativeToNow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache of {Type} with key {Key}", typeof(T), key);
        }
    }
    
    private async Task OpenConnection()
    {
        if (_connectionMultiplexer is not null && _connectionMultiplexer.IsConnected)
        {
            return;
        }

        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionOptions);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }
    }
}
