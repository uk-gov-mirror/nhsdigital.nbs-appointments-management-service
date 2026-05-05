using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Core.Concurrency;
using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance.ReferenceGroups;

public class ReferenceGroupCosmosDocumentStore : IReferenceNumberDocumentStore
{
    private readonly ITypedDocumentCosmosStore<BookingReferenceGroupDocument> _cosmosStore;
    private readonly ICoreReferenceGroupMigrationDocumentStore _numberMigrationDocumentStore;
    private readonly ILeaseManager _leaseManager;
    private readonly IReferenceGroupSelector _referenceGroupSelector;
    private readonly ILogger _logger;
    private readonly string _docType;

    public ReferenceGroupCosmosDocumentStore(
        ITypedDocumentCosmosStore<BookingReferenceGroupDocument> cosmosStore,
        ICoreReferenceGroupMigrationDocumentStore numberMigrationDocumentStore,
        ILeaseManager leaseManager,
        IReferenceGroupSelector referenceGroupSelector,
        ILogger<ReferenceGroupCosmosDocumentStore> logger
    )
    {
        _cosmosStore = cosmosStore;
        _numberMigrationDocumentStore = numberMigrationDocumentStore;
        _leaseManager = leaseManager;
        _referenceGroupSelector = referenceGroupSelector;
        _logger = logger;
        _docType = _cosmosStore.GetDocumentType();
    }

    public async Task<int> AssignReferenceGroup()
    {
        IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments = await GetReferenceGroupDocuments();

        // TODO: Once migration is complete, this code block and associated methods can be removed in the next release.
        if (!IsMigrationComplete(referenceGroupDocuments))
        {
            referenceGroupDocuments = await MigrateDataFromCoreContainerIfNecessary();
        }

        // By this stage we have migrated into the new container where individual reference groups are identified by Id, and there is no "0" entry.
        var referenceGroup = _referenceGroupSelector.Select(referenceGroupDocuments);

        await IncrementSiteCount(referenceGroup);

        return int.Parse(referenceGroup);
    }

    public async Task<int> GetNextSequenceNumber(int referenceGroup)
    {
        ReferenceNumberProvider.EnsureReferenceGroupIsWithinRange(referenceGroup);

        BookingReferenceGroupDocument referenceGroupDocument;
        try
        {
            referenceGroupDocument = await IncrementSequenceForReferenceGroup(referenceGroup);
        }
        catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await MigrateDataFromCoreContainerIfNecessary();
            referenceGroupDocument = await IncrementSequenceForReferenceGroup(referenceGroup);
        }

        _logger.LogInformation("Booking sequence number {sequence} requested for {referenceGroup}", referenceGroupDocument.Sequence, referenceGroup);

        return referenceGroupDocument.Sequence;
    }

    private async Task IncrementSiteCount(string referenceGroup)
    {
        var incrementSiteCountPatch = BookingReferenceGroupDocument.IncrementSiteCount();

        await _cosmosStore.PatchDocument(_docType, referenceGroup, incrementSiteCountPatch);
    }

    private async Task<BookingReferenceGroupDocument> IncrementSequenceForReferenceGroup(int referenceGroup)
    {
        var incrementSequencePatch = BookingReferenceGroupDocument.IncrementSequence();

        return await _cosmosStore.PatchDocument(_docType, referenceGroup.ToString(), incrementSequencePatch);
    }

    private async Task<IEnumerable<BookingReferenceGroupDocument>> MigrateDataFromCoreContainerIfNecessary()
    {
        /*
         * At this stage, WHEN WE CHECKED, migration was not complete, but it might have been in progress, or not started.
         * We do the earlier check first to avoid taking the application-wide lock each time.
         * We now wait behind an application-wide lock.
         * When we can acquire the lock, we need to check again whether migration is complete.
         * If complete, we do nothing, 
         * If still not started, migrate.
         */

        using (var leaseContext = _leaseManager.Acquire(LeaseKeys.ReferenceGroupKey))
        {
            IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments = await GetReferenceGroupDocuments();

            return IsMigrationComplete(referenceGroupDocuments)
                ? referenceGroupDocuments
                : await MigrateDataFromCoreContainer();
        }
    }

    private async Task<BookingReferenceGroupDocument[]> GetReferenceGroupDocuments() => (await _cosmosStore.RunQueryAsync(x => x.DocumentType == _docType)).ToArray();

    private async Task<IEnumerable<BookingReferenceGroupDocument>> MigrateDataFromCoreContainer()
    {
        _logger.LogInformation("{status} migration of reference groups from core container", "Starting");

        var oldCombinedCoreDocument = await _numberMigrationDocumentStore.Get();

        var referenceGroupDocuments = oldCombinedCoreDocument
            .Groups
            .Where(g => g.Prefix > 0) // We no longer need the 0th prefix item, which previously existed to ensure the zero-based indexing worked correctly.
            .Select(g => new BookingReferenceGroupDocument
            {
                DocumentType = _docType,
                Id = g.Prefix.ToString(),
                Sequence = g.Sequence,
                SiteCount = g.SiteCount,
            }).ToList();

        foreach (var referenceGroupDocument in referenceGroupDocuments)
        {
            await _cosmosStore.WriteAsync(referenceGroupDocument);
        }

        _logger.LogInformation("{status} migration of reference groups from core container", "Completed");

        return referenceGroupDocuments;
    }

    private bool IsMigrationComplete(IEnumerable<BookingReferenceGroupDocument> referenceGroupDocuments)
    {
        return referenceGroupDocuments.Count() == ReferenceNumberProvider.NumberOfReferenceGroups;
    }
}
