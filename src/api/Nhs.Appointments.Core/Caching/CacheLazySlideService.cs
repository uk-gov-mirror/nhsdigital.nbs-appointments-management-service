namespace Nhs.Appointments.Core.Caching;

internal class CacheLazySlideService(ICacheStore cacheStore, ICacheLease cacheLease, TimeProvider timeProvider) : ICacheLazySlideService
{
    public async Task<T> InitialiseSlideCache<T>(string key, LazySlideCacheOptions<T> cacheOptions)
    {
        await cacheLease.AquireLease(key);
        var utcNow = timeProvider.GetUtcNow();
        
        var value = await cacheOptions.UpdateOperation();
        
        await SetCacheValue(key, value, utcNow, cacheOptions.AbsoluteExpiration);
        return value;
    }

    public async Task<T> GetSlidingCache<T>(string key, LazySlideCacheObject cacheObject, LazySlideCacheOptions<T> cacheOptions)
    {
        var cacheResponse = await cacheStore.TryGetAsync<LazySlideCacheObject>(key);
        
        ArgumentNullException.ThrowIfNull(cacheResponse);
        
        if (!cacheResponse.Success || )
        {
            cacheStore.
        }

    }

    private async Task SetCacheValue(string key, object value, DateTimeOffset dateTime, TimeSpan absoluteExpiration)
    {
        await cacheStore.SetAsync(key, new LazySlideCacheObject(value, dateTime),
            dateTime.Add(absoluteExpiration));
    }

    private async Task SlideCache<T>(string lazySlideCacheKey, LazySlideCacheOptions<T> options, T lazyValue, DateTimeOffset dateTime)
    {
        try
        {
            //update the cache datetime prematurely so that concurrent waiting threads do not trigger their own slide operation
            await SetCacheValue(lazySlideCacheKey, lazyValue, dateTime, options.SlideExpectedDuration);
        }
        finally
        {
            //can release other threads now that the cache time has been updated
            await cacheLease.ReleaseLease(lazySlideCacheKey);
        }
        
        //then update the actual value now that no locks are being held
        var value = await options.UpdateOperation();
        await SetCacheValue(lazySlideCacheKey, value, dateTime, options.AbsoluteExpiration);
    }
}
