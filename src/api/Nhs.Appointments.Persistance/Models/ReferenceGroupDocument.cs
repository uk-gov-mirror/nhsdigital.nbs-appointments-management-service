using Microsoft.Azure.Cosmos;

namespace Nhs.Appointments.Persistance.Models;

[CosmosDocumentType("reference_group")]
public class CoreReferenceGroupDocument : CoreDataCosmosDocument
{
    public ReferenceGroup[] Groups { get; set; }

    public int GetQuietestReferenceGroup()
    {
        return Groups
            .Where(g => g.Prefix > 0)
            .OrderBy(g => g.SiteCount)
            .ThenBy(g => g.Prefix)
            .First()
            .Prefix;
    }

    public static PatchOperation IncrementSiteCountForReferenceGroup(int referenceGroup)
    {
        return PatchOperation.Increment($"/Groups/{referenceGroup}/SiteCount", 1);
    }

    public static PatchOperation IncrementSequenceForReferenceGroup(int referenceGroup)
    {
        return PatchOperation.Increment($"/Groups/{referenceGroup}/Sequence", 1);
    }

    public int GetSequenceForReferenceGroup(int referenceGroup)
    {
        return Groups
            .Single(gr => gr.Prefix == referenceGroup)
            .Sequence;
    }
}

[CosmosDocumentType("reference_group")]
public class BookingReferenceGroupDocument : BookingReferenceDataCosmosDocument
{
    public int SiteCount { get; set; }
    public int Sequence { get; set; }

    public static PatchOperation IncrementSiteCount()
    {
        return PatchOperation.Increment($"/SiteCount", 1);
    }

    public static PatchOperation IncrementSequence(int increment = 1)
    {
        return PatchOperation.Increment($"/Sequence", increment);
    }
}

public class ReferenceGroup
{
    public int Prefix { get; set; }
    public int SiteCount { get; set; }
    public int Sequence { get; set; }
}
