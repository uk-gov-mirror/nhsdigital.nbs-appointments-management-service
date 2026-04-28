using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.Caching;

public class CacheService(ICacheStore cacheStore, ILeaseManager leaseManager, TimeProvider timeProvider) : ICacheService
{
    public async Task<T> GetLazySlidingCacheValue<T>(string cacheKey, LazySlideCacheOptions<T> options)
    {
        //intentionally prefix a cache key indicating that it is a lazy sliding value
        var lazySlideCacheKey = CacheKey.LazySlideCacheKey(cacheKey);
        
        if (options.AbsoluteExpiration <= options.SlideThreshold)
        {
            throw new ArgumentException("Configuration is not supported, AbsoluteExpiration must be greater than the SlideThreshold");
        }
        
        var cache = await cacheStore.TryGetAsync<LazySlideCacheObject>(lazySlideCacheKey);

        if (!cache.Success)
        {
            return await SlideCache(lazySlideCacheKey, options, timeProvider.GetUtcNow());
        }

        ArgumentNullException.ThrowIfNull(cache.Response);

        if (cache.Response.DueToSlide(options.SlideThreshold, timeProvider.GetUtcNow()))
        {
            _ = SlideCache(lazySlideCacheKey, options, timeProvider.GetUtcNow());
        }
            
        return (T)cache.Response.Value;

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

        using (await leaseManager.AcquireAsync(cacheKey))
        {
            var newValue = await options.UpdateOperation();

            await cacheStore.SetAsync(cacheKey, new CacheObject<T>(newValue), options.AbsoluteExpiration);
            return newValue;
        }
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

    private async Task<T> SlideCache<T>(string lazySlideCacheKey, LazySlideCacheOptions<T> options, DateTimeOffset dateTime)
    {
        using (leaseManager.AcquireAsync(lazySlideCacheKey))
        {
            var cache = await cacheStore.TryGetAsync<LazySlideCacheObject>(lazySlideCacheKey);
            
            if (cache.Success)
            {
                ArgumentNullException.ThrowIfNull(cache.Response);
                if (cache.Response.DueToSlide(options.SlideThreshold, dateTime))
                {
                    _ = cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(cache.Response.Value, dateTime),
                        dateTime.Add(options.AbsoluteExpiration));
                }
                else
                {
                    // Lock acquired and all valid, return cache
                    return (T)cache.Response.Value;
                }
            }
            else
            {
                // must wait for initial cache value
                var slideValue = await options.UpdateOperation();
                await cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(slideValue, dateTime),
                    dateTime.Add(options.AbsoluteExpiration));
                return slideValue;
            }
        }

        // no need to have lock around here as we're silently sliding the cached value
        var setValue = await options.UpdateOperation();
        await cacheStore.SetAsync(lazySlideCacheKey, new LazySlideCacheObject(setValue, dateTime),
            dateTime.Add(options.AbsoluteExpiration));
        return setValue;
    }
}
