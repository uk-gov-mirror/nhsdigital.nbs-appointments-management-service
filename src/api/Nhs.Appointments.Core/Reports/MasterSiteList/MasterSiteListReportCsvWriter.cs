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
        await csvWriter.WriteLineAsync(string.Join(',', MasterSiteListReportMap.Headers()));

        foreach (var site in sites)
        {
            try
            {
                await csvWriter.WriteLineAsync(string.Join(',',
                    CsvFormatter.FormatValue(MasterSiteListReportMap.SiteName(site)),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.OdsCode(site)), 
                    CsvFormatter.FormatValue(MasterSiteListReportMap.SiteType(site)),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.Region(site)),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.ICB(site)),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.Guid(site)),
                    MasterSiteListReportMap.IsDeleted(site),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.Status(site)),
                    MasterSiteListReportMap.Longitude(site),
                    MasterSiteListReportMap.Latitude(site),
                    CsvFormatter.FormatValue(MasterSiteListReportMap.Address(site))
                   ));

            }
            catch(Exception ex)
            {

            }
        }
    }
}
