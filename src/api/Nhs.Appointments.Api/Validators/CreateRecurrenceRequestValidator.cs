using System;
using FluentValidation;
using Nhs.Appointments.Api.Availability;

namespace Nhs.Appointments.Api.Validators;

public class CreateRecurrenceRequestValidator : AbstractValidator<CreateRecurrenceRequest>
{
    public CreateRecurrenceRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Site)            
            .NotEmpty()
            .WithMessage("Provide a valid site");
        RuleFor(x => x.RecurrencePattern)
            .NotEmpty()
            .WithMessage("Provide a recurrence pattern");
        //TODO add more validation
    }
}
