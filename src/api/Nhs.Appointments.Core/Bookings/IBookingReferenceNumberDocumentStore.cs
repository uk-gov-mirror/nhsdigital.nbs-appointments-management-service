namespace Nhs.Appointments.Core.Bookings;

public interface IBookingReferenceNumberDocumentStore
{
    Task<int> AssignReferenceGroup();
    Task<int> GetNextSequenceNumber(int prefix);
}
