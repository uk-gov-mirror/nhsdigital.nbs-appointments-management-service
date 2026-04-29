using StackExchange.Redis;

namespace Nhs.Appointments.Core.Caching.Redis;

internal class RedisConnection(ConfigurationOptions connectionOptions) : IRedisConnection, IAsyncDisposable
{
    private ConnectionMultiplexer _connectionMultiplexer;
    
    public async Task<ConnectionMultiplexer> Get()
    {
        if (_connectionMultiplexer is not null && _connectionMultiplexer.IsConnected)
        {
            return _connectionMultiplexer;
        }

        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionOptions);
        return _connectionMultiplexer;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }
    }
}
