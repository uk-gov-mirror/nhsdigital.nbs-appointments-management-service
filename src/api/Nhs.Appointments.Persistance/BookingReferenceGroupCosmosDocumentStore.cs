using Microsoft.Azure.Cosmos;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance
{
    public class BookingReferenceGroupCosmosDocumentStore : IBookingReferenceNumberDocumentStore
    {
        private const string DocumentId = "main";
        private readonly ITypedDocumentCosmosStore<BookingReferenceGroupDocument> _cosmosStore;
        private readonly ICoreReferenceNumberMigrationDocumentStore _numberMigrationDocumentStore;

        public BookingReferenceGroupCosmosDocumentStore(
            ITypedDocumentCosmosStore<BookingReferenceGroupDocument> cosmosStore, 
            ICoreReferenceNumberMigrationDocumentStore numberMigrationDocumentStore)
        {
            _cosmosStore = cosmosStore;
            _numberMigrationDocumentStore = numberMigrationDocumentStore;
        }

        public async Task<int> AssignReferenceGroup()
        {
            BookingReferenceGroupDocument referenceGroupDocument;
            var docType = _cosmosStore.GetDocumentType();
            try
            {
                referenceGroupDocument = await _cosmosStore.GetByIdAsync<BookingReferenceGroupDocument>("main");
            }
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                referenceGroupDocument = await MigrateFromOldDocument();
            }
            
            var target = referenceGroupDocument!.Groups.Where(g => g.Prefix > 0).OrderBy(g => g.SiteCount).ThenBy(g => g.Prefix).First();
            var siteCountIncrement = PatchOperation.Increment($"/Groups/{target.Prefix}/SiteCount", 1);
            
            await _cosmosStore.PatchDocument(docType, DocumentId, siteCountIncrement);
            return target.Prefix;
        }

        public async Task<int> GetNextSequenceNumber(int prefix)
        {
            var incrementSequencePatch = PatchOperation.Increment($"/Groups/{prefix}/Sequence", 1);
            var docType = _cosmosStore.GetDocumentType();
            BookingReferenceGroupDocument referenceGroupDocument;
            try
            {
                referenceGroupDocument = await _cosmosStore.PatchDocument(docType, DocumentId, incrementSequencePatch);
            }
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                referenceGroupDocument = await MigrateFromOldDocument();
            }
            
            return referenceGroupDocument.Groups.Single(gr => gr.Prefix == prefix).Sequence;
        }

        private async Task<BookingReferenceGroupDocument> MigrateFromOldDocument()
        {
            var docType = _cosmosStore.GetDocumentType();
            var oldDocument = await _numberMigrationDocumentStore.Get();

            var referenceGroupDocument = new BookingReferenceGroupDocument
            {
                DocumentType = docType,
                Id = DocumentId,
                Groups = oldDocument.Groups,
            };

            await _cosmosStore.WriteAsync(referenceGroupDocument);
            
            return referenceGroupDocument;
        }
    }
}
