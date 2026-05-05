using Microsoft.Extensions.Logging;
using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Bookings;

public class ReferenceNumberProvider(
    ISiteStore siteStore,
    IReferenceNumberDocumentStore referenceNumberDocumentStore,
    TimeProvider timeProvider,
    ILogger<ReferenceNumberProvider> logger
        ) : IReferenceNumberProvider
{
    private readonly ISiteStore _siteStore = siteStore;
    private readonly IReferenceNumberDocumentStore _referenceNumberDocumentStore = referenceNumberDocumentStore;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger _logger = logger;

    public const int NumberOfReferenceGroups = 99; 
    public const int MaxSequenceNumber = 999999;

    public async Task<string> GetReferenceNumber(string siteId)
    {
        ArgumentException.ThrowIfNullOrEmpty(siteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(siteId);

        var referenceGroup = await GetReferenceGroup(siteId);

        var sequence = await _referenceNumberDocumentStore.GetNextSequenceNumber(referenceGroup);
        var now = _timeProvider.GetUtcNow();
        var rng = now.Day + now.Second;

        var bookingReferenceNumber = $"{referenceGroup:00}-{rng:00}-{sequence:000000}";

        _logger.LogInformation("Generated booking reference number {bookingReferenceNumber} for site {siteId}", bookingReferenceNumber, siteId);

        return bookingReferenceNumber;
    }

    private async Task<int> GetReferenceGroup(string siteId)
    {
        // TODO: Move this to use the site cache, as this will become the next bottleneck.
        var referenceGroup = await _siteStore.GetReferenceGroup(siteId);
        if (referenceGroup == SiteConstants.UNASSIGNED_REFERENCE_GROUP)
        {
            referenceGroup = await _referenceNumberDocumentStore.AssignReferenceGroup();
            await _siteStore.SaveReferenceGroup(siteId, referenceGroup);
        }

        return referenceGroup;
    }

    public static void EnsureReferenceGroupIsWithinRange(int referenceGroup)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(referenceGroup, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(referenceGroup, ReferenceNumberProvider.NumberOfReferenceGroups);
    }
}
