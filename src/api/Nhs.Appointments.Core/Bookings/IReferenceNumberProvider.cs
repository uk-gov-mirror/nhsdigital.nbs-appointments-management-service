namespace Nhs.Appointments.Core.Bookings;

public interface IReferenceNumberProvider
{
    Task<string> GetReferenceNumber(string siteId);
}
