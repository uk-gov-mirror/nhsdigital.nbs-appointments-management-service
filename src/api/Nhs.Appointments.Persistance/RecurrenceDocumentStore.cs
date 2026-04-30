using Nhs.Appointments.Core.Availability;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public class RecurrenceStore(ITypedDocumentCosmosStore<RecurrenceDocument> documentStore) : IRecurrenceStore
{
    public async Task WriteRecurrenceDocument(string id, int version, string site, RecurrencePattern recurrencePattern,
        string label, RecurrenceException[] recurrenceExceptions)
    {
        var docType = documentStore.GetDocumentType();
        var document = new RecurrenceDocument
        {
            Id = id,
            Version = version,
            Site = site,
            DocumentType = docType,
            RecurrencePattern = recurrencePattern,
            Label = label,
            RecurrenceExceptions = recurrenceExceptions
        };
        await documentStore.WriteAsync(document);
    }
}
