using Microsoft.Azure.Cosmos;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public class ReferenceGroupCosmosDocumentStore(
    ITypedDocumentCosmosStore<BookingReferenceGroupDocument> cosmosStore,
    ICoreReferenceNumberMigrationDocumentStore numberMigrationDocumentStore
    ) : IReferenceNumberDocumentStore
{
    private readonly ITypedDocumentCosmosStore<BookingReferenceGroupDocument> _cosmosStore = cosmosStore;
    private readonly ICoreReferenceNumberMigrationDocumentStore _numberMigrationDocumentStore = numberMigrationDocumentStore;

    public async Task<int> AssignReferenceGroup()
    {
        var docType = _cosmosStore.GetDocumentType();
        IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments = (await _cosmosStore.RunQueryAsync(x => x.DocumentType == docType)).ToArray();

        if (!referenceGroupDocuments.Any())
        {
            referenceGroupDocuments = await MigrateDataFromCoreContainer();
        }

        // By this stage we have migrated into the new container where individual reference groups are identified by Id, and there is no "0" entry.
        var referenceGroup = ReferenceGroupSelector.GetQuietest(referenceGroupDocuments);

        var patchOperation = BookingReferenceGroupDocument.IncrementSiteCount();
        await _cosmosStore.PatchDocument(docType, referenceGroup, patchOperation);

        return int.Parse(referenceGroup);
    }

    public async Task<int> GetNextSequenceNumber(int referenceGroup)
    {
        BookingReferenceGroupDocument referenceGroupDocument;
        try
        {
            referenceGroupDocument = await IncrementSequenceForReferenceGroup(referenceGroup);
        }
        catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await MigrateDataFromCoreContainer();
            referenceGroupDocument = await IncrementSequenceForReferenceGroup(referenceGroup);
        }

        return referenceGroupDocument.Sequence;
    }

    private async Task<BookingReferenceGroupDocument> IncrementSequenceForReferenceGroup(int referenceGroup)
    {
        var docType = _cosmosStore.GetDocumentType();
        var incrementSequencePatch = BookingReferenceGroupDocument.IncrementSequence();

        return await _cosmosStore.PatchDocument(docType, referenceGroup.ToString(), incrementSequencePatch);
    }

    private async Task<IEnumerable<BookingReferenceGroupDocument>> MigrateDataFromCoreContainer()
    {
        // Acquire a lock
        // Check has migration happened already?
            // YES - return
            // No - migrate
        // Release lock
        var docType = _cosmosStore.GetDocumentType();
        var oldCombinedCoreDocument = await _numberMigrationDocumentStore.Get();

        var referenceGroupDocuments = oldCombinedCoreDocument
            .Groups
            .Where(g => g.Prefix > 0)
            .Select(g => new BookingReferenceGroupDocument
            {
                DocumentType = docType,
                Id = g.Prefix.ToString(),
                Sequence = g.Sequence,
                SiteCount = g.SiteCount,
            }).ToList();

        foreach (var referenceGroupDocument in referenceGroupDocuments)
        {
            await _cosmosStore.WriteAsync(referenceGroupDocument);
        }
        
        return referenceGroupDocuments;
    }
}
