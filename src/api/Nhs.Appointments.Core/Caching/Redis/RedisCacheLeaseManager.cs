using Microsoft.Extensions.Logging;

namespace Nhs.Appointments.Core.Caching;

public class RedisCacheLeaseManager(IRedisConnection redisConnection, ILogger<RedisCacheLeaseManager> logger) : ICacheLeaseManager
{
    public async Task<ICacheLeaseContext> Acquire(string key)
    {
        var lockGuid = Guid.NewGuid().ToString();
        var database = redisConnection.Connection.GetDatabase();

        while (!(await database.LockTakeAsync($"{key}:lock", lockGuid, TimeSpan.FromSeconds(1))))
        {
            logger.LogInformation("Waiting to acquire lock for {Key}", key);
            await Task.Delay(TimeSpan.FromMicroseconds(100));
        }
        
        

        return new CacheLeaseContext(key, () => Task.FromResult(mutex.Release()));
    }
}
