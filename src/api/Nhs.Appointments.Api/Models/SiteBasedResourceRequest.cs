namespace Nhs.Appointments.Api.Models;

public record SiteBasedResourceRequest(string Site, bool IgnoreCache, string Scope);
