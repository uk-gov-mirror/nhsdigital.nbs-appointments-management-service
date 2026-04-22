namespace Nhs.Appointments.Persistance.Models;

[CosmosDocumentType("reference_group")]
public class CoreReferenceGroupDocument : CoreDataCosmosDocument
{
    public ReferenceGroup[] Groups { get; set; }
}

[CosmosDocumentType("reference_group")]
public class BookingReferenceGroupDocument : BookingReferenceDataCosmosDocument
{
    public int SiteCount { get; set; }
    public int Sequence { get; set; }
}

public class ReferenceGroup
{
    public int Prefix { get; set; }
    public int SiteCount { get; set; }
    public int Sequence { get; set; }
}
