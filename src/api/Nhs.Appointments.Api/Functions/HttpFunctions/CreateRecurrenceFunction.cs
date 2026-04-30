using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Nhs.Appointments.Api.Auth;
using Nhs.Appointments.Api.Availability;
using Nhs.Appointments.Api.Models;
using Nhs.Appointments.Audit.Functions;
using Nhs.Appointments.Core.Availability;
using Nhs.Appointments.Core.Features;
using Nhs.Appointments.Core.Inspectors;
using Nhs.Appointments.Core.Sites;
using Nhs.Appointments.Core.Users;

namespace Nhs.Appointments.Api.Functions.HttpFunctions;

public class CreateRecurrenceFunction(
    IValidator<CreateRecurrenceRequest> validator,
    IUserContextProvider userContextProvider,
    ILogger<CreateRecurrenceFunction> logger,
    IMetricsRecorder metricsRecorder,
    ISiteService siteService,
    IRecurrenceWriteService recurrenceWriteService,
    IFeatureToggleHelper featureToggleHelper)
    : BaseApiFunction<CreateRecurrenceRequest, Guid>(validator, userContextProvider: userContextProvider, logger,
        metricsRecorder)
{
    [OpenApiOperation("CreateRecurrence", ["Availability"],
        Summary = "Create recurring availability for a date range")]
    [OpenApiRequestBody("application/json", typeof(CreateRecurrenceRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK,
        Description = "Recurrence successfully created")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json",
        typeof(IEnumerable<ErrorMessageResponseItem>), Description = "The body of the request is invalid")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json",
        typeof(ErrorMessageResponseItem), Description = "Unauthorized request to a protected API")]
    [OpenApiResponseWithBody(HttpStatusCode.Forbidden, "application/json", typeof(ErrorMessageResponseItem),
        Description = "Request failed due to insufficient permissions")]
    [RequiresPermission(Permissions.SetupAvailability, typeof(SiteFromBodyInspector))]
    [RequiresAudit(typeof(SiteFromBodyInspector))]
    [Function("CreateRecurrenceFunction")]
    public override async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "availability/create-recurrence")]
        HttpRequest req)
    {
        return await featureToggleHelper.IsFeatureEnabled(Flags.Recurrence)
            ? await base.RunAsync(req)
            : ProblemResponse(HttpStatusCode.NotImplemented, null);
    }

    protected override async Task<ApiResult<Guid>> HandleRequest(CreateRecurrenceRequest request,
        ILogger logger)
    {
        if (await siteService.GetSiteByIdAsync(request.Site) is null)
        {
            return Failed(HttpStatusCode.NotFound, "Site provided was not found.");
        }
        
        var createdRecurrenceId = await recurrenceWriteService.CreateRecurrence(request.Site, request.RecurrencePattern, request.Label, request.RecurrenceExceptions);
        return Success(createdRecurrenceId);
    }
}
