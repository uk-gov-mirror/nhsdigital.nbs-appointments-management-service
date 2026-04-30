using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public class CoreReferenceNumberGroupCosmosDocumentStore(
    ITypedDocumentCosmosStore<CoreReferenceGroupDocument> cosmosStore, 
    IOptions<ReferenceGroupOptions> options
    ) : IReferenceNumberDocumentStore, ICoreReferenceNumberMigrationDocumentStore
{
    private const string DocumentId = "main";
    private readonly ITypedDocumentCosmosStore<CoreReferenceGroupDocument> _cosmosStore = cosmosStore;
    private readonly ReferenceGroupOptions _options = options.Value;

    public async Task<int> AssignReferenceGroup()
    {
        var docType = _cosmosStore.GetDocumentType();
        var referenceGroupDocument = await Get();
        
        var referenceGroup = referenceGroupDocument!.GetQuietestReferenceGroup();
        var patchOperation = CoreReferenceGroupDocument.IncrementSiteCountForReferenceGroup(referenceGroup);
        await _cosmosStore.PatchDocument(docType, DocumentId, patchOperation);

        return referenceGroup;
    }

    public async Task<int> GetNextSequenceNumber(int referenceGroup)
    {
        var docType = _cosmosStore.GetDocumentType();
        var patchOperation = CoreReferenceGroupDocument.IncrementSequenceForReferenceGroup(referenceGroup);
        var referenceGroupDocument = await _cosmosStore.PatchDocument(docType, DocumentId, patchOperation);

        return referenceGroupDocument.GetSequenceForReferenceGroup(referenceGroup);
    }

    public async Task<CoreReferenceGroupDocument> Get()
    {
        CoreReferenceGroupDocument referenceGroupDocument;
        var docType = _cosmosStore.GetDocumentType();
        try
        {
            referenceGroupDocument = await _cosmosStore.GetByIdAsync(DocumentId);
        }
        catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            referenceGroupDocument = new CoreReferenceGroupDocument
            {
                DocumentType = docType,
                Id = DocumentId,
                Groups = Enumerable.Range(0, _options.InitialGroupCount).Select(x => new ReferenceGroup
                {
                    Prefix = x,
                    Sequence = 0,
                    SiteCount = 0
                }).ToArray()
            };

            await _cosmosStore.WriteAsync(referenceGroupDocument);
        }
        
        return referenceGroupDocument;
    }
}
