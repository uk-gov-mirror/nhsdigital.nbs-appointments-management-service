using Newtonsoft.Json;
using Nhs.Appointments.Core.Availability;

namespace Nhs.Appointments.Persistance.Models;

[CosmosDocumentType("recurrence")]
public class RecurrenceDocument : RecurrenceDataCosmosDocument
{
    [JsonProperty("version")]
    public int Version { get; set; }
    
    [JsonProperty("label")]
    public string Label { get; set; }
    
    [JsonProperty("recurrencePattern")]
    public RecurrencePattern RecurrencePattern { get; set; }
    
    [JsonProperty("exceptions")]
    public RecurrenceException[] RecurrenceExceptions { get; set; }
    
    [JsonProperty("createdBy")] 
    public string CreatedBy { get; set; }
    
    [JsonProperty("createdOn")] 
    public DateTime CreatedOn { get; set; }
}
