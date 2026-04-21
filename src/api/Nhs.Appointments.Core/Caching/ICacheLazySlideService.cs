namespace Nhs.Appointments.Core.Caching;

internal interface ICacheLazySlideService
{
    Task<T> InitialiseSlideCache<T>(string key, LazySlideCacheOptions<T> cacheOptions);
    Task<T> SlideCache<T>(string key, LazySlideCacheObject cacheObject, LazySlideCacheOptions<T> cacheOptions);
}
