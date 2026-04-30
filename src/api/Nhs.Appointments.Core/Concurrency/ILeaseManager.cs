
namespace Nhs.Appointments.Core.Concurrency;

public interface ILeaseManager
{
    LeaseManagerMode Mode { get; }
    ILeaseContext Acquire(string leaseKey, LeaseManagerOptions options = null);
    Task<ILeaseContext> AcquireAsync(string leaseKey, LeaseManagerOptions options = null);
}
