using Nhs.Appointments.Core.Availability;

namespace Nhs.Appointments.Core.UnitTests;

public class RecurrenceWriteServiceTests
{
    private readonly Mock<IAvailabilityWriteService> _availabilityWriteService = new();
    private readonly Mock<IRecurrenceStore> _recurrenceStore = new();
    private readonly RecurrenceWriteService _sut;

    public RecurrenceWriteServiceTests()
    {
        _sut = new RecurrenceWriteService(_recurrenceStore.Object, _availabilityWriteService.Object);
    }

    [Fact]
    public async Task CreateRecurrence_CreatesANewRecurrenceDocument_WithAllDetailsInvoked()
    {
        var site = "some-site";
        var label = "My Recurrence Label";
        var recurrencePattern = new RecurrencePattern
        {
            Interval = 1,
            Frequency = "Weekly",
            StartDate = DateOnly.ParseExact("2027-04-02", "yyyy-MM-dd"),
            EndDate = DateOnly.ParseExact("2027-05-20", "yyyy-MM-dd"),
            ByDay = [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
            Session = new Session
            {
                Capacity = 2,
                From = TimeOnly.ParseExact("10:00", "HH:mm"),
                Until = TimeOnly.ParseExact("14:00", "HH:mm"),
                Services = ["Covid", "Flu"],
                SlotLength = 10
            }
        };
        var recurrenceExceptions = new List<RecurrenceException>
        {
            new()
            {
                Date = DateOnly.ParseExact("2027-04-13", "yyyy-MM-dd"),
                OverrideSessionRule =
                    new OverrideSessionRule
                    {
                        From = TimeOnly.ParseExact("09:00", "HH:mm"), Until = TimeOnly.ParseExact("17:00", "HH:mm"),
                    }
            },
            new()
            {
                DateRange = new DateRange
                {
                    StartDate = DateOnly.ParseExact("2027-05-01", "yyyy-MM-dd"),
                    EndDate = DateOnly.ParseExact("2027-05-05", "yyyy-MM-dd"),
                }
            },
            new()
            {
                Date = DateOnly.ParseExact("2027-04-17", "yyyy-MM-dd"),
                OverrideSessionRule = new OverrideSessionRule
                {
                    ServicePatches =
                    [
                        new ServicePatch { Operation = Patch.Add, Service = "MenB" }
                    ]
                }
            }
        };

        _ = await _sut.CreateRecurrence(site, recurrencePattern, label, recurrenceExceptions.ToArray());

        _recurrenceStore.Verify(
            x => x.WriteRecurrenceDocument(It.IsAny<string>(), 1, site, recurrencePattern, label,
                recurrenceExceptions.ToArray()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRecurrence_CreatesANewRecurrenceDocument_WithANewId_ThatIsReturned()
    {
        var capturedStrings = new List<string>();
        
        var response = await _sut.CreateRecurrence(It.IsAny<string>(), It.IsAny<RecurrencePattern>(), It.IsAny<string>(),
            It.IsAny<RecurrenceException[]>());
        
        _recurrenceStore.Verify(
            x => x.WriteRecurrenceDocument(Capture.In(capturedStrings), 1, It.IsAny<string>(),
                It.IsAny<RecurrencePattern>(), It.IsAny<string>(), It.IsAny<RecurrenceException[]>()),
            Times.Once);

        response.Should().Be(capturedStrings.Single());
    }
}
