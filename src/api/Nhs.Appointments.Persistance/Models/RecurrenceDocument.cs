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

public class RecurrencePattern
{
    [JsonProperty("startDate")]
    public DateOnly StartDate { get; set; }
    
    [JsonProperty("endDate")]
    public DateOnly EndDate { get; set; }
    
    [JsonProperty("frequency")]
    public string Frequency { get; set; }
    
    [JsonProperty("interval")]
    public int Interval { get; set; }
    
    [JsonProperty("byDay")]
    public DayOfWeek[] ByDay { get; set; }
    
    [JsonProperty("session")]
    public Session Session { get; set; }
}

public class RecurrenceException
{
    [JsonProperty("label")]
    public string Label { get; set; }
    
    [JsonProperty("date")]
    public DateOnly Date { get; set; }
    
    [JsonProperty("dateRange")]
    public DateRange DateRange { get; set; }
    
    [JsonProperty("session")]
    public OverrideSession OverrideSession { get; set; }
}

public class DateRange
{
    [JsonProperty("startDate")]
    public DateOnly StartDate { get; set; }
    
    [JsonProperty("endDate")]
    public DateOnly EndDate { get; set; }
}

public class OverrideSession
{
    [JsonProperty("from")]
    public TimeOnly From { get; set; }

    [JsonProperty("until")]
    public TimeOnly Until { get; set; }

    [JsonProperty("services")]
    public ServicePatch[] ServicePatches { get; set; }

    [JsonProperty("capacity")]
    public int Capacity { get; set; }
}

public class ServicePatch
{
    [JsonProperty("service")]
    public string Service { get; set; }
    
    [JsonProperty("operation")]
    public PatchEnum Operation { get; set; }
}

public enum PatchEnum
{
    Undefined = 0,
    Add = 1,
    Remove = 2,
}
