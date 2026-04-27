using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nhs.Appointments.Core.Caching.InMemory;
using Nhs.Appointments.Core.Caching.Redis;
using StackExchange.Redis;

namespace Nhs.Appointments.Core.Caching;

public static class ServiceRegistration
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cachingMode = configuration.GetValue<string>("CACHING_MODE") ?? CachingMode.InMemory;

        services.AddTransient<ICacheService, CacheService>();
        
        switch (cachingMode)
        {
            case CachingMode.InMemory: 
                services.AddInMemoryCaching(configuration);
                break;
            case CachingMode.DistributedRedis: 
                services.AddRedisCaching(configuration);
                break;
        }

        return services;
    }
    
    private static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {        
        return services
            .AddSingleton(new ConfigurationOptions()
            {
                EndPoints = { configuration.GetValue<string>("REDIS_ENDPOINT") ?? throw new NullReferenceException("RedisEndpoint not set in configuration") },
                Password = configuration.GetValue<string>("REDIS_PASSWORD") ?? throw new NullReferenceException("RedisPassword not set in configuration"),
            })
            .AddTransient<ICacheStore, RedisCacheStore>();
    }
    
    private static IServiceCollection AddInMemoryCaching(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSingleton<IMemoryCache, MemoryCache>()
            .AddTransient<ICacheStore, InMemoryCacheStore>();
    }
}
