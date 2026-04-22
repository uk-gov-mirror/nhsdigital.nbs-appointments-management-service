using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance
{
    public class BookingReferenceGroupCosmosDocumentStore : IBookingReferenceNumberDocumentStore
    {
        private const string DocumentId = "main";
        private readonly ITypedDocumentCosmosStore<BookingReferenceGroupDocument> _cosmosStore;
        private readonly ICoreReferenceMigrationNumberDocumentStore _migrationDocumentStore;
        private readonly ReferenceGroupOptions _options;

        public BookingReferenceGroupCosmosDocumentStore(
            ITypedDocumentCosmosStore<BookingReferenceGroupDocument> cosmosStore, 
            ICoreReferenceMigrationNumberDocumentStore migrationDocumentStore, 
            IOptions<ReferenceGroupOptions> options)
        {
            _cosmosStore = cosmosStore;
            _migrationDocumentStore = migrationDocumentStore;
            _options = options.Value;
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
                var oldDocument = await _migrationDocumentStore.Get();

                referenceGroupDocument = new BookingReferenceGroupDocument
                {
                    DocumentType = docType,
                    Id = DocumentId,
                    Groups = oldDocument.Groups,
                };

                await _cosmosStore.WriteAsync(referenceGroupDocument);
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
            var referenceGroupDocument = await _cosmosStore.PatchDocument(docType, DocumentId, incrementSequencePatch);
            return referenceGroupDocument.Groups.Single(gr => gr.Prefix == prefix).Sequence;
        }
    }
}
