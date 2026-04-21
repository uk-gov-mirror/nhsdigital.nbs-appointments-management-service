namespace Nhs.Appointments.Core.Caching;

public interface ICacheLease
{
    Task AquireLease(string key);
    Task ReleaseLease(string key);
}
