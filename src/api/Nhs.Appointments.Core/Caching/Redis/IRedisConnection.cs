using StackExchange.Redis;

namespace Nhs.Appointments.Core.Caching;

public interface IRedisConnection
{
    Task<ConnectionMultiplexer> Get();
}
