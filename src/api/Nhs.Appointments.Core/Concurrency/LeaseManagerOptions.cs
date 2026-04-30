namespace Nhs.Appointments.Core.Concurrency;

public class LeaseManagerOptions
{
    public TimeSpan Timeout { get; set; }
    public string Realm { get; set; }
}
