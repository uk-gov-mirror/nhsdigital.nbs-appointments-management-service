using Microsoft.Extensions.Options;

namespace Nhs.Appointments.Core.Concurrency;

internal class InMemoryLeaseManager : ILeaseManager
{
    private readonly Dictionary<string, SemaphoreSlim> _locks;
    private readonly LeaseManagerOptions _options;

    public InMemoryLeaseManager(IOptions<LeaseManagerOptions> options)
    {
        _locks = new Dictionary<string, SemaphoreSlim>();
        _options = options.Value;
    }

    public ILeaseContext Acquire(string key)
    {
        SemaphoreSlim mutex;

        lock (_locks)
        {
            if (!_locks.TryGetValue(key, out var value))
            {
                value = new SemaphoreSlim(1,1);
                _locks.Add(key, value);
            }
            mutex = value;
        }

        if (!mutex.Wait(_options.Timeout))
        {
            throw new AbandonedMutexException($"Abandoned attempt to acquire lock for key {key}");
        }

        return new LeaseContext(key, () => mutex.Release());
    }
}
