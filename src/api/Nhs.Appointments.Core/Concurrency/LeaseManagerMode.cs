namespace Nhs.Appointments.Core.Concurrency;

public static class LeaseManagerMode
{
    public const string InMemory = "InMemory";   
    public const string DistributedAzureBlob = "DistributedAzureBlob";   
}
