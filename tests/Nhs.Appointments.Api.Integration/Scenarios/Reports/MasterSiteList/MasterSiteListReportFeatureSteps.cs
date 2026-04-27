using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FluentAssertions;
using Gherkin.Ast;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Nhs.Appointments.Api.Integration.Collections;
using Nhs.Appointments.Core.Features;
using Nhs.Appointments.Persistance.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Gherkin.Quick;

namespace Nhs.Appointments.Api.Integration.Scenarios.Reports.MasterSiteList;

[Collection(FeatureToggleCollectionNames.ReportsUpliftCollection)]
[FeatureFile("./Scenarios/Reports/MasterSiteList/GetMasterSiteListReport_Enabled.feature")]
public class GetSiteUsersReportFeatureSteps_Enabled()
    : GetMasterSiteListReportFeatureSteps(Flags.ReportsUplift, true);

[Collection(FeatureToggleCollectionNames.ReportsUpliftCollection)]
[FeatureFile("./Scenarios/Reports/MasterSiteList/GetMasterSiteListReport_Disabled.feature")]
public class GetSiteUsersReportFeatureSteps_Disabled()
    : GetMasterSiteListReportFeatureSteps(Flags.ReportsUplift, false);

public abstract class GetMasterSiteListReportFeatureSteps(string flag, bool enabled) : SingleFeatureToggledSteps(flag, enabled), IAsyncLifetime
{
    private string ReportContent { get; set; }

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await CosmosDeleteFeed<SiteDocument>("core_data", sd => sd.Id.Contains(GetTestId), new PartitionKey("site"));
    }

    [When("I request master site list report")]
    public async Task RequestSiteUsersReport()
    {
        var url = $"http://localhost:7071/api/report/master-site-list";

        _response = await GetHttpClientForTest().GetAsync(url);
        _statusCode = _response.StatusCode;
        ReportContent = await _response.Content.ReadAsStringAsync();
    }

    [And("the report has the following headers")]
    public void AssertHeaders(DataTable dataTable)
    {
        var csvHeaders = ReportContent.Split("\n")[0].Trim('\r').Split(",");

        var row = dataTable.Rows.First();
        foreach (var cell in row.Cells)
        {
            csvHeaders.Should().Contain(cell.Value);
        }
    }

    [And("the report contains the following data")]
    public void AssertReportData(DataTable dataTable)
    {
        var rows = dataTable.Rows.Skip(1);

        var textReader = new StringReader(ReportContent);
        var csvReader = new CsvReader(textReader,
            new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, Delimiter = "," });
        var actualData = csvReader.GetRecords<MasterSiteListReportRow>();

        var expectedRows = rows.Select(r => new MasterSiteListReportRow
        {
            SiteName = r.Cells.ElementAt(0).Value,
            OdsCode = r.Cells.ElementAt(1).Value,
            SiteType = r.Cells.ElementAt(2).Value,
            Region = r.Cells.ElementAt(3).Value,
            RegionalName = r.Cells.ElementAt(4).Value,
            ICB = r.Cells.ElementAt(5).Value,
            IcbName = r.Cells.ElementAt(6).Value,
            GUID = r.Cells.ElementAt(7).Value,
            IsDeleted = r.Cells.ElementAt(8).Value,
            Status = r.Cells.ElementAt(9).Value,
            Long = r.Cells.ElementAt(10).Value,
            Lat = r.Cells.ElementAt(11).Value,
            Address = r.Cells.ElementAt(12).Value,
            AccessibleToilet = r.Cells.ElementAt(13).Value,
            BrailleTranslation = r.Cells.ElementAt(14).Value,
            DisabledParking = r.Cells.ElementAt(15).Value,
            CarParking = r.Cells.ElementAt(16).Value,
            InductionLoop = r.Cells.ElementAt(17).Value,
            SignLanguage = r.Cells.ElementAt(18).Value,
            StepFreeAccess = r.Cells.ElementAt(19).Value,
            TextRelay = r.Cells.ElementAt(20).Value,
            WheelchairAccess = r.Cells.ElementAt(21).Value
        });

        var actualReport = actualData.ToList();
        foreach (var expectedRow in expectedRows)
        {
            var realReport = actualReport.FirstOrDefault(a => a.GUID.Contains(expectedRow.GUID));   
            realReport.Should().NotBeNull($"Could not find GUID {expectedRow.GUID} in the CSV. " +
                $"The first GUID found in the file was: {actualReport.FirstOrDefault()?.GUID}");

            realReport.SiteName.Should().Be(expectedRow.SiteName);
            realReport.OdsCode.Should().Be(expectedRow.OdsCode);
            realReport.SiteType.Should().Be(expectedRow.SiteType);
            realReport.Region.Should().Be(expectedRow.Region);
            realReport.RegionalName.Should().Be(expectedRow.RegionalName);
            realReport.ICB.Should().Be(expectedRow.ICB);
            realReport.IcbName.Should().Be(expectedRow.IcbName);
            realReport.IsDeleted.Should().Be(expectedRow.IsDeleted);
            realReport.Status.Should().Be(expectedRow.Status);
            realReport.Lat.Should().Be(expectedRow.Lat);
            realReport.Long.Should().Be(expectedRow.Long);
            realReport.Address.Should().Be(expectedRow.Address);
            realReport.AccessibleToilet.Should().Be(expectedRow.AccessibleToilet);
            realReport.BrailleTranslation.Should().Be(expectedRow.BrailleTranslation);
            realReport.DisabledParking.Should().Be(expectedRow.DisabledParking);
            realReport.CarParking.Should().Be(expectedRow.CarParking);
            realReport.InductionLoop.Should().Be(expectedRow.InductionLoop);
            realReport.SignLanguage.Should().Be(expectedRow.SignLanguage);
            realReport.StepFreeAccess.Should().Be(expectedRow.StepFreeAccess);
            realReport.TextRelay.Should().Be(expectedRow.TextRelay);
            realReport.WheelchairAccess.Should().Be(expectedRow.WheelchairAccess);
        }
    }

    private class MasterSiteListReportRow
    {
        [Name("Site Name")]
        public string SiteName { get; set; }
        [Name("ODS Code")]
        public string OdsCode { get; set; }
        [Name("Site Type")]
        public string SiteType { get; set; }
        [Name("Region")]
        public string Region { get; set; }
        [Name("Regional Name")]
        public string RegionalName { get; set; }
        [Name("ICB")]
        public string ICB { get; set; }
        [Name("ICB Name")] 
        public string IcbName { get; set; }
        [Name("GUID")]
        public string GUID { get; set; }
        [Name("IsDeleted")]
        public string IsDeleted { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("Long")]
        public string Long { get; set; }
        [Name("Lat")]
        public string Lat { get; set; }
        [Name("Address")]
        public string Address { get; set; }
        [Name("Accessible toilet")]
        public string AccessibleToilet { get; set; }
        [Name("Braille translation service")]
        public string BrailleTranslation { get; set; }
        [Name("Disabled car parking")]
        public string DisabledParking { get; set; }
        [Name("Car parking")]
        public string CarParking { get; set; }
        [Name("Induction loop")]
        public string InductionLoop { get; set; }
        [Name("Sign language service")]
        public string SignLanguage { get; set; }
        [Name("Step free access")]
        public string StepFreeAccess { get; set; }
        [Name("Text relay")]
        public string TextRelay { get; set; }
        [Name("Wheelchair access")]
        public string WheelchairAccess { get; set; }
    }
}
