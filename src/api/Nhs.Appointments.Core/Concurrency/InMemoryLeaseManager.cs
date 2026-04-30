using Microsoft.Extensions.Options;

namespace Nhs.Appointments.Core.Concurrency;

internal class InMemoryLeaseManager : ILeaseManager
{
    private readonly Dictionary<string, SemaphoreSlim> _locks;
    private readonly LeaseManagerOptions _defaultOptions;

    public InMemoryLeaseManager(IOptions<LeaseManagerOptions> options)
    {
        _locks = new Dictionary<string, SemaphoreSlim>();
        _defaultOptions = options.Value;
    }

    public LeaseManagerMode Mode => LeaseManagerMode.InMemory;

    public ILeaseContext Acquire(string leaseKey, LeaseManagerOptions options = null)
    {
        var inMemoryLeaseKey = BuildInMemoryKey(leaseKey, options);
        var mutex = ResolveMutex(inMemoryLeaseKey, options);
        if (!mutex.Wait(ResolveTimeout(options)))
        {
            throw new AbandonedMutexException($"Abandoned attempt to acquire lock for lease key {inMemoryLeaseKey}");
        }

        return new LeaseContext(inMemoryLeaseKey, () => mutex.Release());
    }

    public async Task<ILeaseContext> AcquireAsync(string leaseKey, LeaseManagerOptions options = null)
    {
        var inMemoryLeaseKey = BuildInMemoryKey(leaseKey, options);
        var mutex = ResolveMutex(inMemoryLeaseKey, options);

        if (!(await mutex.WaitAsync(ResolveTimeout(options))))
        {
            throw new AbandonedMutexException($"Abandoned attempt to acquire lock for lease key {inMemoryLeaseKey}");
        }

        return new LeaseContext(inMemoryLeaseKey, () => mutex.Release());
    }

    private SemaphoreSlim ResolveMutex(string leaseKey, LeaseManagerOptions options = null)
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
        
        return mutex;
    }

    private string BuildInMemoryKey(string leaseKey, LeaseManagerOptions options = null) =>
        LeaseKeys.InMemoryKeyFactory.Create(options?.Realm ?? _defaultOptions.Realm, leaseKey);
    
    private TimeSpan ResolveTimeout(LeaseManagerOptions options = null) =>
        options?.Timeout ?? _defaultOptions.Timeout;
}
