using Nhs.Appointments.Core.Reports.Helpers;
using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Reports.MasterSiteList;

public class MasterSiteListReportCsvWriter(TimeProvider timeProvider) : IMasterSiteListReportCsvWriter
{
    public async Task<(string fileName, MemoryStream fileContent)> CompileMasterSiteListReportCsv(IEnumerable<Site> sites)
    {
        var fileName = BuildFileName();

        var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await CompileCsv(streamWriter, sites);
        return (fileName, memoryStream);
    }

    private string BuildFileName() =>
    $"MasterSiteListReport_{timeProvider.GetUtcNow():yyyyMMddhhmmss}.csv";

    private async Task CompileCsv(TextWriter csvWriter, IEnumerable<Site> sites)
    {
        var headers = MasterSiteListReportMap.Headers();
        await csvWriter.WriteLineAsync(string.Join(',', headers));

        foreach (var site in sites)
        {
            var row = new List<string>
            {
                CsvFormatter.FormatValue(MasterSiteListReportMap.SiteName(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.OdsCode(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.SiteType(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.Region(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.RegionalName(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.ICB(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.IcbName(site)),
                CsvFormatter.FormatValue(MasterSiteListReportMap.Guid(site)),
                MasterSiteListReportMap.IsDeleted(site).ToString().ToLower(),
                CsvFormatter.FormatValue(MasterSiteListReportMap.Status(site)),
                MasterSiteListReportMap.Longitude(site).ToString(),
                MasterSiteListReportMap.Latitude(site).ToString(),
                CsvFormatter.FormatValue(MasterSiteListReportMap.Address(site))
            };

            // Dynamically add the accessibility values for the remaining headers
            var accessibilityHeaders = headers.Skip(13);
            foreach (var id in accessibilityHeaders)
            {
                row.Add(MasterSiteListReportMap.GetAccessibilityValue(site, id));
            }

            await csvWriter.WriteLineAsync(string.Join(',', row));
        }
    }
}
