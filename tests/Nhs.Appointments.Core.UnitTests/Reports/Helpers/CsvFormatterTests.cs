using Nhs.Appointments.Core.Reports.Helpers;

namespace Nhs.Appointments.Core.UnitTests.Reports.Helpers;

public class CsvFormatterTests
{
    [Theory]
    // Standard cases
    [InlineData("Robin Lane", "\"Robin Lane\"")]
    [InlineData("", "\"\"")]
    [InlineData(null, "\"\"")]

    // Security: Formula Injection neutralization
    [InlineData("=SUM(1,1)", "\"'=SUM(1,1)\"")]
    [InlineData("+44123", "\"'+44123\"")]
    [InlineData("-12.5", "\"'-12.5\"")]
    [InlineData("@username", "\"'@username\"")]

    // These test that even if there are spaces/tabs before the trigger, we still neutralize it.
    [InlineData(" =SUM(1,1)", "\"' =SUM(1,1)\"")]   // Leading space
    [InlineData("  @admin", "\"'  @admin\"")]       // Multiple spaces
    [InlineData("\t-5", "\"'\t-5\"")]                // Leading tab
    [InlineData("\r+10", "\"'\r+10\"")]              // Leading carriage return

    // Integrity: Double quote escaping
    [InlineData("Health \"Express\"", "\"Health \"\"Express\"\"\"")]
    [InlineData("\"Quoted\"", "\"\"\"Quoted\"\"\"")]

    // Combined: Formula + Quotes
    [InlineData("=A1&\"leak\"", "\"'=A1&\"\"leak\"\"\"")]
    public void FormatValue_ShouldNeutralizeAndEscapeCorrectly(string input, string expected)
    {
        // Act
        var result = CsvFormatter.FormatValue(input);

        // Assert
        result.Should().Be(expected);
    }
}
