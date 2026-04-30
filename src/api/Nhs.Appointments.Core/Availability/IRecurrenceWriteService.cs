namespace Nhs.Appointments.Core.Availability;

public interface IRecurrenceWriteService
{
    Task<Guid> CreateRecurrence(string site,
        RecurrencePattern recurrencePattern,
        string label,
        RecurrenceException[] recurrenceExceptions);
}
