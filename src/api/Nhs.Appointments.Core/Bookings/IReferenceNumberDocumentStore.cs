namespace Nhs.Appointments.Core.Bookings;

public interface IReferenceNumberDocumentStore
{
    Task<int> AssignReferenceGroup();
    Task<int> GetNextSequenceNumber(int prefix);
}
