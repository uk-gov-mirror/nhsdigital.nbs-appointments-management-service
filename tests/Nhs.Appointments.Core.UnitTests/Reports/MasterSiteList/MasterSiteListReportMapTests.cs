using FluentAssertions;
using Nhs.Appointments.Core.Reports.MasterSiteList;
using Nhs.Appointments.Core.Sites;
using Xunit;

namespace Nhs.Appointments.Core.UnitTests.Reports.MasterSiteList;

public class MasterSiteListReportMapTests
{
    // Create a shared list of fake definitions for the tests
    private readonly List<AccessibilityDefinition> _fakeDefinitions = new()
    {
        new("accessibility/accessible_toilet", "Accessible toilet"),
        new("accessibility/braille_translation_service", "Braille translation service"),
        new("accessibility/wheelchair_access", "Wheelchair access"),
        new("accessibility/induction_loop", "Induction loop")
    };

    [Fact]
    public void Headers_ReturnsCorrectHeaders_IncludingCleanAccessibilityLabels()
    {
        // Act
        var headers = MasterSiteListReportMap.Headers(_fakeDefinitions);

        // Assert
        // 13 base fields + 4 fake definitions = 17 total
        headers.Should().HaveCount(17);

        // Check base headers
        headers.Should().StartWith(new[] {
            "Site Name", "ODS Code", "Site Type", "Region", "Regional Name",
            "ICB", "ICB Name", "GUID", "IsDeleted", "Status", "Long", "Lat", "Address"
        });

        // Check for specific clean accessibility headers requested in requirements
        headers.Should().Contain("Accessible toilet");
        headers.Should().Contain("Braille translation service");

        // Ensure raw internal IDs are NOT present in the headers
        headers.Should().NotContain("accessibility/wheelchair_access");
    }

    [Fact]
    public void Map_CorrectlyMapsNewFields()
    {
        // Arrange - Use SiteForReport and the clean Site constructor
        var site = new Site(
            Id: "site-guid",
            Name: "Test Site",
            Address: "123 Lane",
            PhoneNumber: "555",
            OdsCode: "ABC01",
            Region: "R1",
            IntegratedCareBoard: "ICB1",
            InformationForCitizens: "",
            Accessibilities: Array.Empty<Accessibility>(),
            location: null,
            status: null,
            isDeleted: false,
            Type: "GP Practice"
        );

        var siteForReport = new SiteForReport(site, "North East", "Hampshire ICB");

        // Act & Assert
        MasterSiteListReportMap.RegionalName(siteForReport).Should().Be("North East");
        MasterSiteListReportMap.IcbName(siteForReport).Should().Be("Hampshire ICB");
    }

    [Fact]
    public void GetAccessibilityValue_ReturnsTrueOnlyWhenValueIsTrue_UsingCleanHeaderNames()
    {
        // Arrange - Clean constructor + SiteForReport
        var site = new Site(
            "1", "S1", "A", "P", "O", "R", "I", "",
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
        var siteForReport = new SiteForReport(site, "Reg Name", "ICB Name");

        // Act & Assert - Pass the definitions
        MasterSiteListReportMap.GetAccessibilityValue(siteForReport, "Wheelchair access", _fakeDefinitions).Should().Be("true");
        MasterSiteListReportMap.GetAccessibilityValue(siteForReport, "Induction loop", _fakeDefinitions).Should().Be("false");
        MasterSiteListReportMap.GetAccessibilityValue(siteForReport, "Non Existent Header", _fakeDefinitions).Should().Be("false");
    }

    [Fact]
    public void Coordinates_ReturnZero_WhenLocationIsNull()
    {
        // Arrange
        var site = new Site("1", "S1", "A", "P", "O", "R", "I", "", null, null, null, false, "");
        var siteForReport = new SiteForReport(site, "RN", "ICBN");

        // Act & Assert
        MasterSiteListReportMap.Longitude(siteForReport).Should().Be(0);
        MasterSiteListReportMap.Latitude(siteForReport).Should().Be(0);
    }
}
