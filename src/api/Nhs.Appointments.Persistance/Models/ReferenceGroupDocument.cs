using Microsoft.Azure.Cosmos;

namespace Nhs.Appointments.Persistance.Models;

[CosmosDocumentType("reference_group")]
public class CoreReferenceGroupDocument : CoreDataCosmosDocument
{
    public ReferenceGroup[] Groups { get; set; }

    public int GetLeastBusyReferenceGroup()
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
        return PatchOperation.Increment(SiteCountPath(referenceGroup), 1);
    }

    public static PatchOperation IncrementSequenceForReferenceGroup(int referenceGroup)
    {
        return PatchOperation.Increment(SequencePath(referenceGroup), 1);
    }

    public int GetSequenceForReferenceGroup(int referenceGroup)
    {
        return Groups
            .Single(gr => gr.Prefix == referenceGroup)
            .Sequence;
    }

    public static string SiteCountPath(int referenceGroup)
    {
        return $"/Groups/{referenceGroup}/SiteCount";
    }

    public static string SequencePath(int referenceGroup) 
    {
        return $"/Groups/{referenceGroup}/Sequence";
    }
}

[CosmosDocumentType("reference_group")]
public class BookingReferenceGroupDocument : BookingReferenceDataCosmosDocument
{
    public const string SiteCountPath = "/SiteCount";
    public const string SequencePath = "/Sequence";

    public int SiteCount { get; set; }

    public int Sequence { get; set; }

    public static PatchOperation IncrementSiteCount()
    {
        return PatchOperation.Increment(SiteCountPath, 1);
    }

    public static PatchOperation IncrementSequence(int increment = 1)
    {
        return PatchOperation.Increment(SequencePath, increment);
    }
}

public class ReferenceGroup
{
    public int Prefix { get; set; }
    public int SiteCount { get; set; }
    public int Sequence { get; set; }
}
