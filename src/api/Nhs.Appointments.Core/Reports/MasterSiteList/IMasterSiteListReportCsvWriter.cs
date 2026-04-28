using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Reports.MasterSiteList;

public interface IMasterSiteListReportCsvWriter
{
    Task<(string fileName, MemoryStream fileContent)> CompileMasterSiteListReportCsv(IEnumerable<SiteForReport> sites, IEnumerable<AccessibilityDefinition> definitions);
}
