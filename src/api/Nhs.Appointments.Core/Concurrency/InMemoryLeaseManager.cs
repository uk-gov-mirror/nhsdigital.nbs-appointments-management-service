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

    public ILeaseContext Acquire(string leaseKey)
    {
        SemaphoreSlim mutex;

        lock (_locks)
        {
            if (!_locks.ContainsKey(leaseKey))
            {
                _locks.Add(leaseKey, new SemaphoreSlim(1,1));
            }
            mutex = _locks[leaseKey];
        }

        if (!mutex.Wait(_options.Timeout))
        {
            throw new AbandonedMutexException($"Abandoned attempt to acquire lock for lease key {leaseKey}");
        }

        return new LeaseContext(leaseKey, () => mutex.Release());
    }
}
