namespace Nhs.Appointments.Core.Caching;

public interface ICacheLeaseManager
{
    Task<ICacheLeaseContext> Acquire(string key);
}
