using Nhs.Appointments.Core.Caching;
using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Bookings;

public interface IReferenceNumberProvider
{
    Task<string> GetReferenceNumber(string siteId);
}

public class ReferenceNumberProvider : IReferenceNumberProvider
{
    private readonly ISiteService _siteService;
    private readonly ICacheService _cacheService;
    private readonly IReferenceNumberDocumentStore _referenceNumberDocumentStore;
    private readonly TimeProvider _timeProvider;
    public ReferenceNumberProvider(
        ISiteService siteService,
        ICacheService cacheService,
        IReferenceNumberDocumentStore referenceNumberDocumentStore,
        TimeProvider timeProvider)
    {
        _siteService = siteService;
        _cacheService = cacheService;
        _referenceNumberDocumentStore = referenceNumberDocumentStore;
        _timeProvider = timeProvider;
    }
    public async Task<string> GetReferenceNumber(string siteId)
    {
        var referenceNumberGroup =
            await _cacheService.GetCacheValue(
                $"site:referenceNumberGroup:{siteId}", 
                new CacheOptions<int>(
                    async () => await GetSitesAssignedReferenceGroup(siteId), 
                    TimeSpan.FromHours(24)));

        var sequence = await _referenceNumberDocumentStore.GetNextSequenceNumber(referenceNumberGroup);
        var now = _timeProvider.GetUtcNow();
        var rng = now.Day + now.Second;

        return $"{referenceNumberGroup:00}-{rng:00}-{sequence:000000}";
    }
    
    private async Task<int> GetSitesAssignedReferenceGroup(string siteId)
    {
        var site = await _siteService.GetSiteByIdAsync(siteId, true);
        if (!SiteHasNotBeenAssignedReferenceGroup(site))
        {
            return site.ReferenceNumberGroup;
        }

        var referenceGroup = await _referenceNumberDocumentStore.AssignReferenceGroup();
        await _siteService.AssignPrefix(siteId, referenceGroup);

        return referenceGroup;

    }
    
    private static bool SiteHasNotBeenAssignedReferenceGroup(Site site) => site.ReferenceNumberGroup == 0;
}

public interface IReferenceNumberDocumentStore
{
    Task<int> AssignReferenceGroup();
    Task<int> GetNextSequenceNumber(int prefix);
}
