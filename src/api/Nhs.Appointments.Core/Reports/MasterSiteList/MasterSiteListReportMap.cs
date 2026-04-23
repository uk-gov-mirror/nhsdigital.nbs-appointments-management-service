using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Reports.MasterSiteList;

public static class MasterSiteListReportMap
{
    public static string[] Headers()
    {
        var headers = new List<string>
        {
            "Site Name", "ODS Code", "Site Type", "Region", "Regional Name",
            "ICB", "ICB Name", "GUID", "IsDeleted", "Status", "Long", "Lat", "Address"
        };

        // We use the Values from our map to ensure clean, required headers only
        headers.AddRange(AccessibilityMapping.Values);

        return headers.ToArray();
    }

    public static string SiteName(SiteForReport site) => site.Name;
    public static string OdsCode(SiteForReport site) => site.OdsCode;
    public static string SiteType(SiteForReport site) => site.Type;
    public static string Region(SiteForReport site) => site.Region;
    public static string RegionalName(SiteForReport site) => site.RegionalName;
    public static string ICB(SiteForReport site) => site.IntegratedCareBoard;
    public static string IcbName(SiteForReport site) => site.IntegratedCareBoardName;
    public static string Guid(SiteForReport site) => site.Id;
    public static bool IsDeleted(SiteForReport site) => site.isDeleted ?? false;
    public static string Status(SiteForReport site) => site.status?.ToString() ?? SiteStatus.Online.ToString();

    public static double Longitude(SiteForReport site) => site.Coordinates?.Longitude ?? 0;
    public static double Latitude(SiteForReport site) => site.Coordinates?.Latitude ?? 0;
    public static string Address(SiteForReport site) => site.Address;

    public static string GetAccessibilityValue(SiteForReport site, string headerName)
    {
        // Find the raw ID (e.g. 'accessibility/wheelchair_access') 
        // that matches the required header (e.g. 'Wheelchair access')
        var id = AccessibilityMapping.FirstOrDefault(x => x.Value == headerName).Key;

        if (id == null)
        {
            return "false";
        }

        var match = site.Accessibilities?.FirstOrDefault(a => a.Id == id);
        return (match != null && match.Value == "true") ? "true" : "false";
    }
}
