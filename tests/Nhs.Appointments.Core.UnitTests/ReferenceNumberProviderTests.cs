using Microsoft.Extensions.Logging;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.UnitTests;

public class ReferenceNumberProviderTests
{
    private readonly ReferenceNumberProvider _sut;
    private readonly Mock<ISiteStore> _siteStore = new();
    private readonly Mock<IReferenceNumberDocumentStore> _referenceNumberDocumentStore = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly Mock<ILogger<ReferenceNumberProvider>> _logger = new();
    private readonly Random _random = new();

    public ReferenceNumberProviderTests()
    {
        _sut = new ReferenceNumberProvider(_siteStore.Object, _referenceNumberDocumentStore.Object, _timeProvider.Object, _logger.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    [InlineData("    ")]
    public async Task GetReferenceNumber_ThrowsArgumentException_WhenSiteIdIsNotSupplied(string siteId)
    {
        // Arrange - not required.

        // Act.
        Func<Task<string>> action = async () => await _sut.GetReferenceNumber(siteId);

        // Assert.
        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*(Parameter 'siteId')");
    }

    [Fact]
    public async Task GetReferenceNumber_AssignsRefGroup_WhenNotCurrentlyAssigned()
    {
        // Arrange.
        var randomReferenceGroup = _random.Next(1, 100);
        var randomSiteId = Guid.NewGuid().ToString();

        _siteStore.Setup(x => x.GetReferenceGroup(randomSiteId)).ReturnsAsync(SiteConstants.UNASSIGNED_REFERENCE_GROUP);
        _referenceNumberDocumentStore.Setup(x => x.AssignReferenceGroup()).ReturnsAsync(randomReferenceGroup);

        // Act.
        await _sut.GetReferenceNumber(randomSiteId);

        // Assert.
        _referenceNumberDocumentStore.Verify(x => x.AssignReferenceGroup(), Times.Once());
        _siteStore.Verify(x => x.SaveReferenceGroup(randomSiteId, randomReferenceGroup), Times.Once());
    }

    [Fact]
    public async Task GetReferenceNumber_DoesNotAssignsRefGroup_WhenAlreadyAssigned()
    {
        // Arrange.
        var randomReferenceGroup = _random.Next(1, 100);
        var randomSiteId = Guid.NewGuid().ToString();

        _siteStore.Setup(x => x.GetReferenceGroup(randomSiteId)).ReturnsAsync(randomReferenceGroup);

        // Act.
        await _sut.GetReferenceNumber(randomSiteId);

        // Assert.
        _referenceNumberDocumentStore.Verify(x => x.AssignReferenceGroup(), Times.Never);
        _siteStore.Verify(x => x.SaveReferenceGroup(randomSiteId, It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetReferenceNumber_GeneratesCorrectlyFormattedNumber()
    {
        // Arrange.
        var randomReferenceGroup = _random.Next(1, 100);
        var randomSiteId = Guid.NewGuid().ToString();
        var fixedDayOfMonth = 31;
        var fixedSecondOfMinute = 59;
        var expectedRandomPortion = fixedDayOfMonth + fixedSecondOfMinute;
        var randomSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);

        _timeProvider.Setup(x => x.GetUtcNow()).Returns(new DateTime(2077, 1, fixedDayOfMonth, 9, 0, fixedSecondOfMinute));
        _referenceNumberDocumentStore.Setup(x => x.GetNextSequenceNumber(randomReferenceGroup)).ReturnsAsync(randomSequence);

        _siteStore.Setup(x => x.GetReferenceGroup(randomSiteId)).ReturnsAsync(randomReferenceGroup);

        // Act.
        var result = await _sut.GetReferenceNumber(randomSiteId);

        // Assert.
        var expectedBookingReferenceNumber = $"{randomReferenceGroup:00}-{expectedRandomPortion:00}-{randomSequence:000000}";
        result.Should().Be(expectedBookingReferenceNumber);
    }

    [Fact]
    public async Task GetReferenceNumber_LogsGeneratedNumber()
    {
        // Arrange.
        var randomReferenceGroup = _random.Next(1, 100);
        var randomSiteId = Guid.NewGuid().ToString();
        var fixedDayOfMonth = 31;
        var fixedSecondOfMinute = 59;
        var expectedRandomPortion = fixedDayOfMonth + fixedSecondOfMinute;
        var randomSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);

        _timeProvider.Setup(x => x.GetUtcNow()).Returns(new DateTime(2077, 1, fixedDayOfMonth, 9, 0, fixedSecondOfMinute));
        _referenceNumberDocumentStore.Setup(x => x.GetNextSequenceNumber(randomReferenceGroup)).ReturnsAsync(randomSequence);

        _siteStore.Setup(x => x.GetReferenceGroup(randomSiteId)).ReturnsAsync(randomReferenceGroup);

        // Act.
        var result = await _sut.GetReferenceNumber(randomSiteId);

        // Assert.
        var expectedBookingReferenceNumber = $"{randomReferenceGroup:00}-{expectedRandomPortion:00}-{randomSequence:000000}";

        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains($"Generated booking reference number {expectedBookingReferenceNumber} for site {randomSiteId}")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once
        );
    }
}
