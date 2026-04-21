using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Reports.MasterSiteList;

public static class MasterSiteListReportMap
{
    // These are the exact IDs from your CSV mapped to the "Wanted" names
    private static readonly Dictionary<string, string> AccessibilityMapping = new()
    {
        { "accessibility/accessible_toilet", "Accessible toilet" },
        { "accessibility/braille_translation_service", "Braille translation service" },
        { "accessibility/disabled_car_parking", "Disabled car parking" },
        { "accessibility/car_parking", "Car parking" },
        { "accessibility/induction_loop", "Induction loop" },
        { "accessibility/sign_language_service", "Sign language service" },
        { "accessibility/step_free_access", "Step free access" },
        { "accessibility/text_relay", "Text relay" },
        { "accessibility/wheelchair_access", "Wheelchair access" }
    };

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

    public static string SiteName(Site site) => site.Name;
    public static string OdsCode(Site site) => site.OdsCode;
    public static string SiteType(Site site) => site.Type;
    public static string Region(Site site) => site.Region;
    public static string RegionalName(Site site) => site.RegionalName;
    public static string ICB(Site site) => site.IntegratedCareBoard;
    public static string IcbName(Site site) => site.IntegratedCareBoardName;
    public static string Guid(Site site) => site.Id;
    public static bool IsDeleted(Site site) => site.isDeleted ?? false;
    public static string Status(Site site) => site.status?.ToString() ?? SiteStatus.Online.ToString();

    public static double Longitude(Site site) => site.Coordinates?.Longitude ?? 0;
    public static double Latitude(Site site) => site.Coordinates?.Latitude ?? 0;
    public static string Address(Site site) => site.Address;

    public static string GetAccessibilityValue(Site site, string headerName)
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
