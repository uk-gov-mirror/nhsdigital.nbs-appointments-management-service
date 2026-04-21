namespace Nhs.Appointments.Core.Caching;

public interface ICacheLeaseContext : IAsyncDisposable
{
    string CacheKey { get; }
}
