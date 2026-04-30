using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Nhs.Appointments.Core.Blob;
using Nhs.Appointments.Core.Concurrency;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddConcurrency(this IServiceCollection services, IConfiguration configuration)
    {
        var leaseManagerConnection = configuration.GetValue<string>("LEASE_MANAGER_CONNECTION");

        services
            .AddTransient<ILeaseManagerFactory, LeaseManagerFactory>()
            .Configure<LeaseManagerOptions>(opts =>
            {
                opts.Timeout =
                    TimeSpan.FromSeconds(configuration.GetValue("LEASE_MANAGER_DEFAULT_TIME_OUT_SECONDS", 15));
                opts.Realm = configuration.GetValue("LEASE_MANAGER_DEFAULT_REALM", "leases");
            })
            .AddSingleton<ILeaseManager, InMemoryLeaseManager>();
        
        if (!string.IsNullOrEmpty(leaseManagerConnection))
        {
            services.AddAzureBlobStoreLeasing(leaseManagerConnection);
        }
        
        return services;
    }

    private static IServiceCollection AddAzureBlobStoreLeasing(this IServiceCollection services, string connectionString)
    {
        services.AddAzureClients(x =>
        {
            x.AddBlobServiceClient(connectionString);
        });

        return services
            .AddTransient<IAzureBlobStorage, AzureBlobStorage>()
            .AddTransient<ILeaseManager, AzureStorageLeaseManager>();
    }
}
