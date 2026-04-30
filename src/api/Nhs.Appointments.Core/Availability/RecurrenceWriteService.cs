namespace Nhs.Appointments.Core.Availability;

public class RecurrenceWriteService(
    IRecurrenceStore recurrenceStore,
    IAvailabilityWriteService availabilityWriteService) : IRecurrenceWriteService
{
    public async Task<Guid> CreateRecurrence(string site, RecurrencePattern recurrencePattern, string label,
        RecurrenceException[] recurrenceExceptions)
    {
        var recurrenceId = Guid.NewGuid();
        await recurrenceStore.WriteRecurrenceDocument(recurrenceId.ToString(), 1, site, recurrencePattern, label,
            recurrenceExceptions);

        //TODO calculate daily_documents to be written and create them

        return recurrenceId;
    }
}
