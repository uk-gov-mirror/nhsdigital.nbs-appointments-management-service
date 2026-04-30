using Newtonsoft.Json;
using Nhs.Appointments.Core.Availability;

namespace Nhs.Appointments.Api.Availability;

public record CreateRecurrenceRequest(
    [property:JsonProperty("site", Required = Required.Always)]
    string Site,
    [property:JsonProperty("recurrencePattern", Required = Required.Always)]
    RecurrencePattern RecurrencePattern,
    [property:JsonProperty("label")]
    string Label,
    [property:JsonProperty("exceptions")]
    RecurrenceException[] RecurrenceExceptions
);
