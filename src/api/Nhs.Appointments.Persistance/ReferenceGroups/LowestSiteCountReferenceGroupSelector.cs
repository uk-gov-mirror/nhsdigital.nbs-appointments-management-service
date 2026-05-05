using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance.ReferenceGroups;

/// <summary>
/// This class contains logic for assigning a reference group to a site by chossing the first one with the lowest site count.
/// </summary>
public class LowestSiteCountReferenceGroupSelector : IReferenceGroupSelector
{
    public string Select(IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments)
    {
        EnsureDocumentsAreValid(referenceGroupDocuments);

        return referenceGroupDocuments
                .OrderBy(g => g.SiteCount)
                .ThenBy(g => g.Id)
                .First()
                .Id;
    }

    private static void EnsureDocumentsAreValid(IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments)
    {
        ArgumentNullException.ThrowIfNull(referenceGroupDocuments, nameof(referenceGroupDocuments));
        if (!referenceGroupDocuments.Any())
        {
            throw new ArgumentException("At least one reference group document must be supplied.", nameof(referenceGroupDocuments));
        }
    }
}
