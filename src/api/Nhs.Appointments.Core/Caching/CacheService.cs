namespace Nhs.Appointments.Core.Caching;

public class CacheService(ICacheStore cacheStore, ICacheLazySlideService cacheLazySlideService, TimeProvider timeProvider) : ICacheService
{
    public async Task<T> GetLazySlidingCacheValue<T>(string cacheKey, LazySlideCacheOptions<T> options)
    {
        //intentionally prefix cache key indicating that it is a lazy sliding value
        var lazySlideCacheKey = CacheKey.LazySlideCacheKey(cacheKey);
        
        if (options.AbsoluteExpiration <= options.SlideThreshold)
        {
            throw new ArgumentException("Configuration is not supported, AbsoluteExpiration must be greater than the SlideThreshold");
        }
        
        var cache = await cacheStore.TryGetAsync<LazySlideCacheObject>(cacheKey);

        if (cache.Success)
        {
            
        }
        
        return await cacheLazySlideService.SlideCache(lazySlideCacheKey, cache.o)

        var utcNow = timeProvider.GetUtcNow();
        
        await cacheLease.AquireLease(lazySlideCacheKey);

        var slidePerformed = false;

        try
        {
            var cache = await cacheStore.TryGetAsync<LazySlideCacheObject>(cacheKey);
            if (cache.Success)
            {
                ArgumentNullException.ThrowIfNull(cache.Response);
            
                //check if we want to update the existing cache in the background lazily...
                if (cache.Response.DateTimeUpdated.Add(options.SlideThreshold) < utcNow)
                {
                    //Sliding cache functionality
                    
                    //Update the cache value so the NEXT request gets a newer version of the latest expensive value fetch
                    //This approach means the cache entry is never guaranteed to be the exact latest value (unless a cache value does not exist) - but it is recent enough to not have a big impact
                    //The performance gain is a sufficient benefit to the value being potentially slightly behind the latest value
                    _ = SlideCache(lazySlideCacheKey, options, (T)cache.Response.Value, utcNow);
                    slidePerformed = true;
                }
            
                //return the current cached value regardless of whether sliding was invoked
                return (T)cache.Response.Value;
            }
            
            var value = await options.UpdateOperation();
            await cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(value, utcNow),
                utcNow.Add(options.AbsoluteExpiration));
            return value;
        }
        finally
        {
            //the lock was released earlier if a slide was performed
            if (!slidePerformed)
            {
                await cacheLease.ReleaseLease(lazySlideCacheKey);
            }
        }
    }

    public async Task<T> GetCacheValue<T>(string cacheKey, CacheOptions<T> options)
    {
        var cache = await cacheStore.TryGetAsync<CacheObject<T>>(cacheKey);
        if (cache.Success)
        {
            ArgumentNullException.ThrowIfNull(cache.Response);
            if (cache.Response.Value != null)
            {
                return cache.Response.Value;
            }
        }
        
        var newValue = await options.UpdateOperation();

        await cacheStore.SetAsync(cacheKey, new CacheObject<T>(newValue), options.AbsoluteExpiration);
        return newValue;
    }

    public async Task<T> GetCacheValueWithDefault<T>(string cacheKey, CacheOptions<T> options, T defaultValue)
    {
        var cache = await cacheStore.TryGetAsync<CacheObject<T>>(cacheKey);
        if (cache.Success)
        {
            ArgumentNullException.ThrowIfNull(cache.Response);
            if (cache.Response.Value != null)
            {
                return cache.Response.Value;
            }
        }
        
        var tryResult = await TryPattern.TryAsync(options.UpdateOperation);
            
        if (!tryResult.Completed)
        {
            return defaultValue;
        }

        await cacheStore.SetAsync(cacheKey, new CacheObject<T>(tryResult.Result), options.AbsoluteExpiration);
        return tryResult.Result;
    }

    private async Task SlideCache<T>(string lazySlideCacheKey, LazySlideCacheOptions<T> options, T lazyValue, DateTimeOffset dateTime)
    {
        try
        {
            //update the cache datetime prematurely so that concurrent waiting threads do not trigger their own slide operation
            await cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(lazyValue, dateTime),
                dateTime.Add(options.AbsoluteExpiration));
        }
        finally
        {
            //can release other threads now that the cache time has been updated
            await cacheLease.ReleaseLease(lazySlideCacheKey);
        }
        
        //then update the actual value now that no locks are being held
        var value = await options.UpdateOperation();
        await cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(value, dateTime),
            dateTime.Add(options.AbsoluteExpiration));
    }
}
