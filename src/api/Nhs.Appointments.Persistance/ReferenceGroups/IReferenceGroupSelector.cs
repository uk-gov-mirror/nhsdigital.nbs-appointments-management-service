using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance.ReferenceGroups;

public interface IReferenceGroupSelector
{
    string Select(IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments);
}
