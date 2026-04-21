using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Bookings;

public interface IReferenceNumberProvider
{
    Task<string> GetReferenceNumber(string siteId);
}

public class ReferenceNumberProvider : IReferenceNumberProvider
{
    private readonly ISiteService _siteService;
    private readonly IReferenceNumberDocumentStore _referenceNumberDocumentStore;
    private readonly TimeProvider _timeProvider;
    public ReferenceNumberProvider(
        ISiteService siteService,
        IReferenceNumberDocumentStore referenceNumberDocumentStore,
        TimeProvider timeProvider)
    {
        _siteService = siteService;
        _referenceNumberDocumentStore = referenceNumberDocumentStore;
        _timeProvider = timeProvider;
    }
    public async Task<string> GetReferenceNumber(string siteId)
    {        
        var site = await _siteService.GetSiteByIdAsync(siteId);
        if (site.ReferenceNumberGroup == 0)
        {
            var referenceGroup = await _referenceNumberDocumentStore.AssignReferenceGroup();
            await _siteService.AssignPrefix(siteId, referenceGroup);
        }

        var sequence = await _referenceNumberDocumentStore.GetNextSequenceNumber(site.ReferenceNumberGroup);
        var now = _timeProvider.GetUtcNow();
        var rng = now.Day + now.Second;

        return $"{site.ReferenceNumberGroup:00}-{rng:00}-{sequence:000000}";
    }
}

public interface IReferenceNumberDocumentStore
{
    Task<int> AssignReferenceGroup();
    Task<int> GetNextSequenceNumber(int prefix);
}
