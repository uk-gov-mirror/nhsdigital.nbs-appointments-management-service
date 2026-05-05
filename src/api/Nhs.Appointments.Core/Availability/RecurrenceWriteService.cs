namespace Nhs.Appointments.Core.Availability;

public class RecurrenceWriteService(
    IRecurrenceStore recurrenceStore,
    IAvailabilityWriteService availabilityWriteService) : IRecurrenceWriteService
{
    public async Task<Guid> CreateRecurrence(string site, RecurrencePattern recurrencePattern, string label,
        RecurrenceException[] recurrenceExceptions)
    {
        var recurrenceId = Guid.NewGuid();
        const int recurrenceVersion = 1;
        await recurrenceStore.WriteRecurrenceDocument(recurrenceId.ToString(), recurrenceVersion, site,
            recurrencePattern, label,
            recurrenceExceptions);

        var recurringDailyAvailability = CalculateRecurrenceDailyAvailability(recurrenceId,
            recurrenceVersion, 
            label, 
            recurrencePattern, 
            recurrenceExceptions);

        var documentTasks = recurringDailyAvailability.Select(x =>
            availabilityWriteService.SetAvailabilityAsync(x.Date, site, x.Sessions, ApplyAvailabilityMode.Additive));
        
        //fire off all tasks together as they impact different days and have separate locks
        //TODO gracefully handle single failure??
        await Task.WhenAll(documentTasks);

        return recurrenceId;
    }

    private List<DailyAvailability> CalculateRecurrenceDailyAvailability(Guid recurrenceId, int recurrenceVersion, string label,
        RecurrencePattern recurrencePattern, RecurrenceException[] recurrenceExceptions)
    {
        recurrencePattern.Session.RecurrenceId = recurrenceId;
        recurrencePattern.Session.RecurrenceVersion = recurrenceVersion;

        var datesInRecurrence = GetDatesInRecurrence(recurrencePattern.StartDate, recurrencePattern.EndDate,
            recurrencePattern.ByDay);

        var exceptionDates = GetRecurrenceExceptionDates(recurrenceExceptions);
        var overrideDates = GetRecurrenceOverrides(recurrencePattern, recurrenceExceptions);

        return datesInRecurrence.Select(date => GenerateDailyAvailability(date, recurrencePattern.Session, exceptionDates, overrideDates)).ToList();
    }
    
    private static DailyAvailability GenerateDailyAvailability(
        DateOnly date, 
        Session parentSession,
        IEnumerable<DateOnly> exceptionDates,
        IEnumerable<DailyAvailability> overrides)
    {
        var dailyAvailability = new DailyAvailability
        {
            Date = date
        };
            
        if (exceptionDates.Contains(date))
        {
            dailyAvailability.Sessions = [];
            return dailyAvailability;
        }
        
        var overrideDate = overrides.SingleOrDefault(x => x.Date == date);

        if (overrideDate != null)
        {
            dailyAvailability.Sessions = overrideDate.Sessions;
            return dailyAvailability;
        }
        
        dailyAvailability.Sessions = [parentSession];
        return dailyAvailability;
    }

    private static IEnumerable<DateOnly> GetDatesInRecurrence(DateOnly start, DateOnly end,
        params DayOfWeek[] daysOfWeek)
    {
        var cursor = start;
        while (cursor <= end)
        {
            if (daysOfWeek.Contains(cursor.DayOfWeek))
            {
                yield return cursor;
            }

            cursor = cursor.AddDays(1);
        }
    }

    /// <summary>
    ///     Get all dates where the exceptions dictate that there is NO recurring availability on that day/range
    /// </summary>
    /// <param name="recurrenceExceptions"></param>
    /// <returns></returns>
    private static IEnumerable<DateOnly> GetRecurrenceExceptionDates(RecurrenceException[] recurrenceExceptions)
    {
        var exceptionDates = new List<DateOnly>();

        var exceptionRules = recurrenceExceptions.Where(x => x.OverrideSessionRule == null);

        //these rules can overlap and intersect and that's okay
        foreach (var exceptionRule in exceptionRules)
        {
            switch (exceptionRule.Date)
            {
                case null:
                    var datesInRange = GetAllDatesBetween(exceptionRule.DateRange.StartDate,
                        exceptionRule.DateRange.EndDate);
                    exceptionDates.AddRange(datesInRange);
                    break;
                default:
                    exceptionDates.Add(exceptionRule.Date.Value);
                    break;
            }
        }

        //return ordered distinct list once all rules applied
        return exceptionDates.Distinct().Order();
    }

    /// <summary>
    ///     Get all dates where the exceptions dictate that there is an override to the parent rule on a specific date
    /// </summary>
    /// <param name="recurrencePattern"></param>
    /// <param name="recurrenceExceptions"></param>
    /// <returns></returns>
    private static IEnumerable<DailyAvailability> GetRecurrenceOverrides(RecurrencePattern recurrencePattern,
        RecurrenceException[] recurrenceExceptions)
    {
        var overrideDates = new List<DailyAvailability>();

        var overrideRules = recurrenceExceptions.Where(x => x.OverrideSessionRule != null);

        //these rules can NOT overlap dates (i.e. no hierarchy of overrides)
        //dateRanges are also NOT supported for overrides
        foreach (var overrideRule in overrideRules)
        {
            switch (overrideRule.Date)
            {
                case null:
                    throw new NotSupportedException("Recurrence overrides not supported for date ranges");
                default:
                    overrideDates.Add(new DailyAvailability
                    {
                        Date = overrideRule.Date.Value,
                        Sessions =
                        [
                            GenerateOverriddenSession(recurrencePattern.Session, overrideRule.OverrideSessionRule)
                        ]
                    });
                    break;
            }
        }

        return overrideDates.OrderBy(x => x.Date);
    }

    private static Session GenerateOverriddenSession(Session parentSession, OverrideSessionRule overrideSessionRule)
    {
        return new Session
        {
            From = overrideSessionRule.From ?? parentSession.From,
            Until = overrideSessionRule.Until ?? parentSession.Until,
            Capacity = overrideSessionRule.Capacity ?? parentSession.Capacity,
            Label = overrideSessionRule.Label ?? parentSession.Label,
            
            Services = ApplyOverrideServicePatches(parentSession.Services, overrideSessionRule.ServicePatches),
            
            RecurrenceId = parentSession.RecurrenceId,
            RecurrenceVersion = parentSession.RecurrenceVersion,
            SlotLength = parentSession.SlotLength,
        };
    }

    private static string[] ApplyOverrideServicePatches(string[] parentServices, ServicePatch[] servicePatches)
    {
        var servicesList = parentServices.ToList();
        foreach (var servicePatch in servicePatches)
        {
            switch (servicePatch.Operation)
            {
                case Patch.Add:
                    servicesList.Add(servicePatch.Service);
                    break;
                case Patch.Remove:
                    servicesList.Remove(servicePatch.Service);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //distinct list to remove any duplicated services that may have been added
        return servicesList.Distinct().ToArray();
    }

    private static IEnumerable<DateOnly> GetAllDatesBetween(DateOnly start, DateOnly end)
    {
        var cursor = start;
        while (cursor <= end)
        {
            yield return cursor;
            cursor = cursor.AddDays(1);
        }
    }
}
