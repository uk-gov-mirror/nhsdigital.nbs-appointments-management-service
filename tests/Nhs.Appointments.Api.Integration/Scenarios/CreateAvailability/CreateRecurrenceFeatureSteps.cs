using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Gherkin.Ast;
using Microsoft.Azure.Cosmos;
using Nhs.Appointments.Api.Availability;
using Nhs.Appointments.Api.Integration.Collections;
using Nhs.Appointments.Api.Json;
using Nhs.Appointments.Core.Availability;
using Nhs.Appointments.Core.Features;
using Nhs.Appointments.Persistance.Models;
using Xunit;
using Xunit.Gherkin.Quick;

namespace Nhs.Appointments.Api.Integration.Scenarios.CreateAvailability
{
    public abstract class CreateRecurrenceFeatureSteps(string flag, bool enabled) : SingleFeatureToggledSteps(flag, enabled)
    {
        private Guid LastCreatedRecurrenceId { get; set; }
        
        [When("I create the following recurring availability at the default site")]
        [And("I create the following recurring availability at the default site")]
        public async Task CreateRecurrence(DataTable dataTable)
        {
            var request = dataTable.Rows.Skip(1).Take(1).Select(row =>
            {
                var recurrencePattern = new RecurrencePattern
                {
                    StartDate = dataTable.GetExactDateRowValueOrNaturalLanguageOrDefault(row, "StartDate"),
                    EndDate = dataTable.GetExactDateRowValueOrNaturalLanguageOrDefault(row, "EndDate"),
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
            (_, LastCreatedRecurrenceId) = await JsonRequestReader.ReadRequestAsync<Guid>(await _response.Content.ReadAsStreamAsync());
        }
        
        [Then("the following latest created recurring availability exists at the default site")]
        [And("the following latest created recurring availability exists at the default site")]
        public async Task AssertRecurrenceDocument(DataTable dataTable)
        {
            var expectedDocument = dataTable.Rows.Skip(1).Take(1).Select(row =>
            {
                var recurrencePattern = new RecurrencePattern
                {
                    StartDate = dataTable.GetExactDateRowValueOrNaturalLanguageOrDefault(row, "StartDate"),
                    EndDate = dataTable.GetExactDateRowValueOrNaturalLanguageOrDefault(row, "EndDate"),
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

                return new RecurrenceDocument
                {
                    Id = LastCreatedRecurrenceId.ToString(),
                    DocumentType = "recurrence",
                    Version = dataTable.GetIntRowValueOrDefault(row, "Version", 1),
                    Site = GetSiteId(dataTable.GetRowValueOrDefault(row, "Site")),
                    RecurrencePattern = recurrencePattern,
                    RecurrenceExceptions = null,
                    Label = dataTable.GetRowValueOrDefault(row, "Label"),
                    LastUpdatedBy = _userId
                };
            }).Single();
            
            var recurrenceDocument = await CosmosReadItem<RecurrenceDocument>("recurrence_data", expectedDocument.Id,
                new PartitionKey(expectedDocument.Site), CancellationToken.None);

            recurrenceDocument.Resource.Should().BeEquivalentTo(expectedDocument, x => x.Excluding(y => y.LastUpdatedOn));
            recurrenceDocument.Resource.LastUpdatedOn.Should().NotBeNull();
        }
    }

    [Collection(FeatureToggleCollectionNames.RecurrenceCollection)]
    [FeatureFile("./Scenarios/CreateAvailability/CreateRecurrence_Disabled.feature")]
    public class CreateRecurrenceFeatureSteps_Disabled() : CreateRecurrenceFeatureSteps(Flags.Recurrence, false);

    [Collection(FeatureToggleCollectionNames.RecurrenceCollection)]
    [FeatureFile("./Scenarios/CreateAvailability/CreateRecurrence_Enabled.feature")]
    public class CreateRecurrenceFeatureSteps_Enabled() : CreateRecurrenceFeatureSteps(Flags.Recurrence, true);
}
