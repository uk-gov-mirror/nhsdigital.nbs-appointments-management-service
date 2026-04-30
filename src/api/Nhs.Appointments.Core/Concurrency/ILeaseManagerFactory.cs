namespace Nhs.Appointments.Core.Concurrency;

public interface ILeaseManagerFactory
{
    ILeaseManager Create(string mode = null);
}
