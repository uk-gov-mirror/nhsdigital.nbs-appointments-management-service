using System;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Api.Availability;
using Nhs.Appointments.Core.Availability;

namespace Nhs.Appointments.Api.Validators;

public class CreateRecurrenceRequestValidator : AbstractValidator<CreateRecurrenceRequest>
{
    public CreateRecurrenceRequestValidator(TimeProvider timeProvider, IOptions<ChangeAvailabilityOptions> options)
    {
        RuleFor(x => x.Site)            
            .NotEmpty()
            .WithMessage("Provide a valid site");
        RuleFor(x => x.RecurrencePattern)
            .NotEmpty()
            .WithMessage("Provide a recurrence pattern")
            .SetValidator(new RecurrencePatternValidator(timeProvider, options));
        
        //TODO RecurrenceExceptionRules!
    }
}

public class RecurrencePatternValidator : AbstractValidator<RecurrencePattern>
{
    public RecurrencePatternValidator(TimeProvider timeProvider, IOptions<ChangeAvailabilityOptions> options)
    {
        //do we rename this var if its being used more than one place?
        var maxDays = options.Value.CancelADateRangeMaximumDays;
        
        RuleFor(x => x.StartDate).Cascade(CascadeMode.Stop)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("End date must be after Start date")
            .GreaterThanOrEqualTo(DateOnly.Parse(timeProvider.GetUtcNow().AddDays(1).ToString("yyyy-MM-dd")))
            .WithMessage("Start date must be at least 1 day in the future");

        RuleFor(x => x.EndDate)
            .Must((req, endDate) => WithinMaxDays(endDate, req.StartDate, maxDays))
            .WithMessage($"End date has to be less than {maxDays} days after the Start date.");
        
        RuleFor(x => x.Session)
            .NotEmpty()
            .WithMessage("Provide a valid session")
            .SetValidator(new SessionValidator());
        
        RuleFor(x => x.ByDay)
            .NotEmpty()
            .WithMessage("Provide at least one day")
            .Must(SpecifyEachDayOnlyOnce)
            .WithMessage("A day can only appear once");
        
        RuleFor(x => x.Frequency)
            .Must(s => s == "Weekly")
            .WithMessage("Only weekly frequency is currently supported");
        
        RuleFor(x => x.Interval)
            .Must(s => s == 1)
            .WithMessage("Only single recurring interval is currently supported");
    }
    
    private static bool WithinMaxDays(DateOnly until, DateOnly from, int maxDays)
    {
        return until < from.AddDays(maxDays);
    }
    
    private static bool SpecifyEachDayOnlyOnce(DayOfWeek[] days)
    {
        var allDays = days.ToList();
        return allDays.Count == allDays.Distinct().Count();
    }
}

