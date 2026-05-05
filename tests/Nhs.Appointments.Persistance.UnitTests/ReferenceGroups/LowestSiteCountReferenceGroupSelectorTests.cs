using FluentAssertions;
using Nhs.Appointments.Persistance.Models;
using Nhs.Appointments.Persistance.ReferenceGroups;

namespace Nhs.Appointments.Persistance.UnitTests.ReferenceGroups;

public class LowestSiteCountReferenceGroupSelectorTests
{
    private readonly Random _random = new();
    private readonly LowestSiteCountReferenceGroupSelector _sut = new();

    [Fact]
    public void ImplementsIReferenceGroupSelector()
    {
        // Arrange - not required.

        // Act - not required.

        // Assert.
        typeof(IReferenceGroupSelector).IsAssignableFrom(typeof(LowestSiteCountReferenceGroupSelector)).Should().BeTrue();
    }

    [Fact]
    public void Select_ThrowsArgumentException_WhenReferenceGroupDocumentsIsNull()
    {
        // Arrange - not required.

        // Act.
        Action action = () => _sut.Select(null!);

        // Assert.
        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("referenceGroupDocuments")
            .WithMessage("Value cannot be null*");
    }

    [Fact]
    public void Select_ThrowsArgumentException_WhenReferenceGroupDocumentsAreEmpty()
    {
        // Arrange - not required.

        // Act.
        Action action = () => _sut.Select([]);

        // Assert.
        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("referenceGroupDocuments")
            .WithMessage("At least one reference group document must be supplied.*");
    }

    [Fact]
    public void Select_ReturnsReferenceGroupWithLowestSiteCount_WhenSiteCountsAreUnique()
    {
        // Arrange.
        var lowestSiteCount = _random.Next(1, 140);
        var referenceGroupDocuments = new List<BookingReferenceGroupDocument>
        {
            new() {
                Id = "47",
                SiteCount = lowestSiteCount * 2,
                Sequence = 35
            },
            new() {
                Id = "22",
                SiteCount = lowestSiteCount * 3,
                Sequence = 40
            },
            new() {
                Id = "36",
                SiteCount = lowestSiteCount,
                Sequence = 17
            }
        };

        // Act.
        var referenceGroup = _sut.Select(referenceGroupDocuments);

        // Assert.
        referenceGroup.Should()
            .Be(referenceGroupDocuments.Single(rgd => rgd.SiteCount == lowestSiteCount).Id);
    }

    [Fact]
    public void Select_ReturnsReferenceGroupWithLowestSiteCountAndLowestId_WhenSiteCountsAreNonUnique()
    {
        // Arrange.
        var matchingLowestSiteCount = _random.Next(1, 140);
        var referenceGroupDocuments = new List<BookingReferenceGroupDocument>
        {
            new() {
                Id = "47",
                SiteCount = matchingLowestSiteCount,
                Sequence = 35
            },
            new() {
                Id = "22",
                SiteCount = matchingLowestSiteCount,
                Sequence = 40
            },
            new() {
                Id = "36",
                SiteCount = matchingLowestSiteCount * 2,
                Sequence = 17
            }
        };

        // Act.
        var referenceGroup = _sut.Select(referenceGroupDocuments);

        // Assert.
        referenceGroup.Should().Be("22");
    }
}
