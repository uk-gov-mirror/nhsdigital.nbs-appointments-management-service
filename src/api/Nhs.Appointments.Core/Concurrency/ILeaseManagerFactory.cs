namespace Nhs.Appointments.Core.Concurrency;

public interface ILeaseManagerFactory
{
    ILeaseManager Create(LeaseManagerMode? mode = null);
}
