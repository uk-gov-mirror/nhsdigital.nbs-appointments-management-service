namespace Nhs.Appointments.Core.Availability;

public interface IRecurrenceStore
{
    Task WriteRecurrenceDocument(string id, int version, string site, RecurrencePattern recurrencePattern, string label,
        RecurrenceException[] recurrenceExceptions);
}
