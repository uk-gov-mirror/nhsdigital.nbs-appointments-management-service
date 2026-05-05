using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Gherkin.Ast;
using Nhs.Appointments.Api.Availability;
using Nhs.Appointments.Api.Integration.Collections;
using Nhs.Appointments.Api.Json;
using Nhs.Appointments.Core.Availability;
using Nhs.Appointments.Core.Features;
using Xunit;
using Xunit.Gherkin.Quick;

namespace Nhs.Appointments.Api.Integration.Scenarios.CreateAvailability
{
    public abstract class CreateRecurrenceFeatureSteps(string flag, bool enabled) : SingleFeatureToggledSteps(flag, enabled)
    {
        [When("I create the following recurring availability at the default site")]
        [And("I create the following recurring availability at the default site")]
        public async Task CreateRecurrence(DataTable dataTable)
        {
            var request = dataTable.Rows.Skip(1).Take(1).Select(row =>
            {
                var recurrencePattern = new RecurrencePattern
                {
                    StartDate = dataTable.GetNaturalLanguageDateRowValueOrDefault(row, "StartDate"),
                    EndDate = dataTable.GetNaturalLanguageDateRowValueOrDefault(row, "EndDate"),
                    ByDay = dataTable.GetRowValueOrDefault(row, "ByDay").Split(",").Select(x => Enum.Parse<DayOfWeek>(x.Trim())).ToArray(),
                    Session = new Session
                    {
                        From = TimeOnly.Parse(dataTable.GetRowValueOrDefault(row, "From")),
                        Until = TimeOnly.Parse(dataTable.GetRowValueOrDefault(row, "Until")),
                        Services = dataTable.GetRowValueOrDefault(row, "Services").Split(",").Select(s => s.Trim()).ToArray(),
                        SlotLength = dataTable.GetIntRowValueOrDefault(row, "SlotLength", 10),
                        Capacity = dataTable.GetIntRowValueOrDefault(row, "Capacity", 1)
                    },
                    Frequency = "Weekly",
                    Interval = 1
                };
            
                return new CreateRecurrenceRequest(GetSiteId(dataTable.GetRowValueOrDefault(row, "Site")), recurrencePattern, dataTable.GetRowValueOrDefault(row, "Label"), null);
            }).Single();

            var payload = JsonResponseWriter.Serialize(request);
            _response = await GetHttpClientForTest().PostAsync("http://localhost:7071/api/availability/create-recurrence",
                new StringContent(payload));
            _statusCode = _response.StatusCode;
        }
    }
    
    [Collection(FeatureToggleCollectionNames.RecurrenceCollection)]
    [FeatureFile("./Scenarios/CreateAvailability/CreateRecurrence_Disabled.feature")]
    public class CreateRecurrenceFeatureSteps_Disabled() : CreateRecurrenceFeatureSteps(Flags.Recurrence, false)
    {
    }

    [Collection(FeatureToggleCollectionNames.RecurrenceCollection)]
    [FeatureFile("./Scenarios/CreateAvailability/CreateRecurrence_Enabled.feature")]
    public class CreateRecurrenceFeatureSteps_Enabled() : CreateRecurrenceFeatureSteps(Flags.Recurrence, true)
    {
    }
}
