using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.UnitTests;

public class ReferenceNumberProviderTests
{
    private readonly ReferenceNumberProvider _sut;
    private readonly Mock<ISiteService> _siteService = new();
    private readonly Mock<IReferenceNumberDocumentStore> _referenceNumberDocumentStore = new();
    private readonly Mock<TimeProvider> _timeProvider = new();

    public ReferenceNumberProviderTests()
    {
        _sut = new ReferenceNumberProvider(_siteService.Object, _referenceNumberDocumentStore.Object, _timeProvider.Object);
    }

    [Fact]
    public async Task GetReferenceNumber_AssignsRefGroup_WhenNotCurrentAssigned()
    {
        _siteService.Setup(x => x.GetSiteByIdAsync("test", It.IsAny<string>())).ReturnsAsync(new Site("test", "NAME", "ADDRESS", 
            "PHONENUMBER", "ODSCODE", "REGION", "ICB", "INFO", 
            new List<Accessibility>(), new Location("",[0, 0]), SiteStatus.Online, false, "TYPE", 0));
        _referenceNumberDocumentStore.Setup(x => x.AssignReferenceGroup()).ReturnsAsync(14);

        await _sut.GetReferenceNumber("test");

        _referenceNumberDocumentStore.Verify(x => x.AssignReferenceGroup(), Times.Once());
        _siteService.Verify(x => x.AssignPrefix("test", 14), Times.Once());
    }

    [Fact]
    public async Task GetReferenceNumber_DoesNotAssignsRefGroup_WhenAlreadyAssigned()
    {
        _siteService.Setup(x => x.GetSiteByIdAsync("test", It.IsAny<string>())).ReturnsAsync(new Site(
            "test", "NAME", "ADDRESS", "PHONENUMBER", "ODSCODE", "REGION", 
            "ICB", "INFO", new List<Accessibility>(), new Location("",
            [0, 0]), SiteStatus.Online, false, "TYPE", 14));

        await _sut.GetReferenceNumber("test");

        _referenceNumberDocumentStore.Verify(x => x.AssignReferenceGroup(), Times.Never);
        _siteService.Verify(x => x.AssignPrefix("test", It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetReferenceNumber_GeneratesCorrectlyFormattedNumber()
    {
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(new DateTime(2077, 1, 31, 9, 0, 59));
        _referenceNumberDocumentStore.Setup(x => x.GetNextSequenceNumber(14)).ReturnsAsync(2345);

        _siteService.Setup(x => x.GetSiteByIdAsync("test", It.IsAny<string>())).ReturnsAsync(new Site(
            "test", "NAME", "ADDRESS", "PHONENUMBER", "ODSCODE", "REGION", 
            "ICB", "INFO", new List<Accessibility>(), new Location("",
                [0, 0]), SiteStatus.Online, false, "TYPE", 14));

        var result = await _sut.GetReferenceNumber("test");
        result.Should().Be("14-90-002345");
    }
}
