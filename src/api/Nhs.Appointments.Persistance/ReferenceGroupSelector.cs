using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public static class ReferenceGroupSelector
{
    public static string GetLeastBusy(IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments)
    {
        return referenceGroupDocuments
                .OrderBy(g => g.SiteCount)
                .ThenBy(g => g.Id)
                .First()
                .Id;
    }
}
