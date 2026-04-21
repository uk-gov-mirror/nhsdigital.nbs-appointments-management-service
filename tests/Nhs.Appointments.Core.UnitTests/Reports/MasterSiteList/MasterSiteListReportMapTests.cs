using FluentAssertions;
using Nhs.Appointments.Core.Reports.MasterSiteList;
using Nhs.Appointments.Core.Sites;
using Xunit;

namespace Nhs.Appointments.Core.UnitTests.Reports.MasterSiteList;

public class MasterSiteListReportMapTests
{
    [Fact]
    public void Headers_ReturnsCorrectHeaders_IncludingCleanAccessibilityLabels()
    {
        // Act
        var headers = MasterSiteListReportMap.Headers();

        // Assert
        // 13 base fields + 9 accessibility fields = 22 total
        headers.Should().HaveCount(22);

        // Check base headers
        headers.Should().StartWith(new[] {
            "Site Name", "ODS Code", "Site Type", "Region", "Regional Name",
            "ICB", "ICB Name", "GUID", "IsDeleted", "Status", "Long", "Lat", "Address"
        });

        // Check for specific clean accessibility headers requested in requirements
        headers.Should().Contain("Accessible toilet");
        headers.Should().Contain("Braille translation service");
        headers.Should().Contain("Wheelchair access");

        // Ensure raw internal IDs are NOT present in the headers
        headers.Should().NotContain("accessibility/wheelchair_access");
    }

    [Fact]
    public void Map_CorrectlyMapsNewFields()
    {
        // Arrange
        var site = new Site(
            Id: "site-guid",
            Name: "Test Site",
            Address: "123 Lane",
            PhoneNumber: "555",
            OdsCode: "ABC01",
            Region: "R1",
            IntegratedCareBoard: "ICB1",
            RegionalName: "North East",
            IntegratedCareBoardName: "Hampshire ICB",
            InformationForCitizens: "",
            Accessibilities: Array.Empty<Accessibility>(),
            location: null,
            status: null,
            isDeleted: false,
            Type: "GP Practice"
        );

        // Act & Assert
        MasterSiteListReportMap.RegionalName(site).Should().Be("North East");
        MasterSiteListReportMap.IcbName(site).Should().Be("Hampshire ICB");
        MasterSiteListReportMap.Region(site).Should().Be("R1");
        MasterSiteListReportMap.ICB(site).Should().Be("ICB1");
    }

    [Fact]
    public void GetAccessibilityValue_ReturnsTrueOnlyWhenValueIsTrue_UsingCleanHeaderNames()
    {
        // Arrange
        var site = new Site(
            "1", "S1", "A", "P", "O", "R", "I", "Reg Name", "ICB Name",
            "",
            new[]
            {
                new Accessibility("accessibility/wheelchair_access", "true"),
                new Accessibility("accessibility/induction_loop", "false")
            },
            null,
            null,
            false,
            "Type"
        );

        // Act & Assert
        // We now pass the "Clean" header name because that's what the mapper expects
        MasterSiteListReportMap.GetAccessibilityValue(site, "Wheelchair access").Should().Be("true");
        MasterSiteListReportMap.GetAccessibilityValue(site, "Induction loop").Should().Be("false");

        // Check a header that isn't in our dictionary or the site
        MasterSiteListReportMap.GetAccessibilityValue(site, "Non Existent Header").Should().Be("false");
    }

    [Fact]
    public void Coordinates_ReturnZero_WhenLocationIsNull()
    {
        // Arrange
        var site = new Site("1", "S1", "A", "P", "O", "R", "I", "RN", "ICBN", "", null, null, null, false, "");

        // Act & Assert
        MasterSiteListReportMap.Longitude(site).Should().Be(0);
        MasterSiteListReportMap.Latitude(site).Should().Be(0);
    }
}
