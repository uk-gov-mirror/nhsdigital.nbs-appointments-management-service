using Microsoft.Extensions.Hosting;
using Nhs.Appointments.Api;
using Nhs.Appointments.Api.Auth;
using Nhs.Appointments.Api.Features;
using Nhs.Appointments.Core.Logger;
using Nhs.Appointments.Api.Middleware;
using Nhs.Appointments.Audit;
using Nhs.Appointments.Core.Configuration;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddMyaConfiguration();
    })
    .ConfigureFeatureDependencies()
    .ConfigureFunctionsWebApplication((context, builder) =>
    {
        builder
            .UseMiddleware<TypeDecoratorMiddleware>()
            .UseMiddleware<AuthenticationMiddleware>()
            .UseMiddleware<AuthorizationMiddleware>()
            .UseMiddleware<NoCacheMiddleware>()
            .AddAudit()
            .ConfigureFunctionDependencies(context.Configuration);
    })
    .UseAppointmentsSerilog()
    .Build();

host.Run();
