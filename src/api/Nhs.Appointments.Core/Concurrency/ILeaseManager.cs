
namespace Nhs.Appointments.Core.Concurrency;

public interface ILeaseManager
{
    ILeaseContext Acquire(string key);
}
