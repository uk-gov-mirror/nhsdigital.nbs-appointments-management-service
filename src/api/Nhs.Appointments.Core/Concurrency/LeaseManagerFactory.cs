namespace Nhs.Appointments.Core.Concurrency;

public class LeaseManagerFactory : ILeaseManagerFactory
{
    private readonly ILeaseManager[] _leaseManagers;
    
    public LeaseManagerFactory(IEnumerable<ILeaseManager> leaseManagers)
    {
        var manager = leaseManagers.ToArray();
        if (manager.Length == 0)
        {
            throw new ArgumentException("No lease managers have been registered");
        }

        _leaseManagers = manager;
    }

    public ILeaseManager Create(LeaseManagerMode? mode = null)
    {
        if (mode is null)
        {
            return ResolveDefault();
        }

        return _leaseManagers.SingleOrDefault(x => x.Mode.Equals(mode)) ?? throw new ArgumentException($"No lease manager found for mode: {mode}");
    }
    
    private ILeaseManager ResolveDefault() => 
        _leaseManagers.SingleOrDefault(x => x.Mode.Equals(LeaseManagerMode.DistributedAzureBlob)) 
        ?? _leaseManagers.SingleOrDefault(x => x.Mode.Equals(LeaseManagerMode.InMemory)) 
        ?? throw new ArgumentException("No default lease manager found");
}
