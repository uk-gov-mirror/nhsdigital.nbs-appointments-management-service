namespace Nhs.Appointments.Core.Concurrency;

public interface ILeaseContext : IDisposable
{
    string LeaseKey { get; }
}
