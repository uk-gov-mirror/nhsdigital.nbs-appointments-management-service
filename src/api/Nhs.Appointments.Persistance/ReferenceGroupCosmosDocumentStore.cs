using Microsoft.Azure.Cosmos;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance
{
    public class ReferenceGroupCosmosDocumentStore : IReferenceNumberDocumentStore
    {
        private readonly ITypedDocumentCosmosStore<BookingReferenceGroupDocument> _cosmosStore;
        private readonly ICoreReferenceNumberMigrationDocumentStore _numberMigrationDocumentStore;

        public ReferenceGroupCosmosDocumentStore(
            ITypedDocumentCosmosStore<BookingReferenceGroupDocument> cosmosStore, 
            ICoreReferenceNumberMigrationDocumentStore numberMigrationDocumentStore)
        {
            _cosmosStore = cosmosStore;
            _numberMigrationDocumentStore = numberMigrationDocumentStore;
        }

        public async Task<int> AssignReferenceGroup()
        {
            IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments;
            var docType = _cosmosStore.GetDocumentType();

            referenceGroupDocuments = (await _cosmosStore.RunQueryAsync<BookingReferenceGroupDocument>(x => x.DocumentType == docType)).ToArray();

            if (!referenceGroupDocuments.Any())
            {
                referenceGroupDocuments = await MigrateFromAllOldDocument();
            }
            
            var target = referenceGroupDocuments.Where(g => g.Id != 0.ToString()).OrderBy(g => g.SiteCount).ThenBy(g => g.Id).First();
            var siteCountIncrement = PatchOperation.Increment($"/SiteCount", 1);
            
            await _cosmosStore.PatchDocument(docType, target.Id, siteCountIncrement);
            return int.Parse(target.Id);
        }

        public async Task<int> GetNextSequenceNumber(int prefix)
        {
            var incrementSequencePatch = PatchOperation.Increment($"/Sequence", 1);
            var docType = _cosmosStore.GetDocumentType();
            BookingReferenceGroupDocument referenceGroupDocument;
            try
            {
                referenceGroupDocument = await _cosmosStore.PatchDocument(docType, prefix.ToString(), incrementSequencePatch);
            }
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await MigrateFromAllOldDocument();
                referenceGroupDocument = await _cosmosStore.PatchDocument(docType, prefix.ToString(), incrementSequencePatch);
            }
            
            return referenceGroupDocument.Sequence;
        }

        private async Task<IEnumerable<BookingReferenceGroupDocument>> MigrateFromAllOldDocument()
        {
            // Acquire a lock
            // Check has migration happened already?
                // YES - return
                // No - migrate
            // Release lock
            var docType = _cosmosStore.GetDocumentType();
            var oldDocument = await _numberMigrationDocumentStore.Get();

            var referenceGroupDocuments = oldDocument.Groups.Select(g => new BookingReferenceGroupDocument
            {
                DocumentType = docType,
                Id = g.Prefix.ToString(),
                Sequence = g.Prefix,
                SiteCount = g.SiteCount,
            }).ToList();

            foreach (var referenceGroupDocument in referenceGroupDocuments)
            {
                await _cosmosStore.WriteAsync(referenceGroupDocument);
            }
            
            return referenceGroupDocuments;
        }
    }
}
