namespace Nhs.Appointments.Core.Caching;

public class CacheLeaseContext : ICacheLeaseContext
{
    private readonly Func<Task> _release;

    public CacheLeaseContext(string key, Func<Task> release)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        ArgumentNullException.ThrowIfNull(release, nameof(release));

        CacheKey = key;
        _release = release;
    }
    
    public string CacheKey { get; }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(_release());
    }

}
