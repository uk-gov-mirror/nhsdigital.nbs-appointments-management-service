using Newtonsoft.Json;

namespace Nhs.Appointments.Api.Availability;

public record CreateRecurrenceRequest(
    [property:JsonProperty("site", Required = Required.Always)]
    string Site
    //TODO add content
);
