using StackExchange.Redis;

namespace Nhs.Appointments.Core.Caching;

public interface IRedisConnection
{
    ConnectionMultiplexer Connection { get; }
}
