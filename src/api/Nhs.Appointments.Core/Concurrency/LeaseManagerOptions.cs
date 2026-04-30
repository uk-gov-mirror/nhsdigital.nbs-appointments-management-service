namespace Nhs.Appointments.Core.Concurrency;

public class LeaseManagerOptions
{
    public TimeSpan Timeout { get; set; }
    public string ContainerName { get; set; }
}
