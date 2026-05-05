namespace Nhs.Appointments.Core.Concurrency;

public class LeaseContext : ILeaseContext
{
    private readonly Action _release;

    public LeaseContext(string leaseKey, Action release)
    {
        ArgumentException.ThrowIfNullOrEmpty(leaseKey, nameof(leaseKey));
        ArgumentNullException.ThrowIfNull(release, nameof(release));

        LeaseKey = leaseKey;
        _release = release;
    }

    public void Dispose()
    {
        _release();
    }

    public string LeaseKey { get; private set; }
}
