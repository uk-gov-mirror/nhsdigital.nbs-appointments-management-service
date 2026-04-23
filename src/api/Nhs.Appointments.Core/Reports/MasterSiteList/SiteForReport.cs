using Nhs.Appointments.Core.Sites;

namespace Nhs.Appointments.Core.Reports.MasterSiteList;

public record SiteForReport(
    Site Site,
    string RegionalName,
    string IntegratedCareBoardName) : Site(Site.Id, Site.Name, Site.Address, Site.PhoneNumber, Site.OdsCode, Site.Region, Site.IntegratedCareBoard, Site.InformationForCitizens, Site.Accessibilities, Site.location, Site.status, Site.isDeleted, Site.Type);
