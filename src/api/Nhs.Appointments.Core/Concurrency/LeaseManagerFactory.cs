namespace Nhs.Appointments.Core.Concurrency;

public class LeaseManagerFactory : ILeaseManagerFactory
{
    private readonly IEnumerable<ILeaseManager> _leaseManagers;
    
    public LeaseManagerFactory(IEnumerable<ILeaseManager> leaseManagers)
    {
        _leaseManagers = leaseManagers;
    }

    public ILeaseManager Create(string mode = null)
    {
        if (string.IsNullOrEmpty(mode))
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
