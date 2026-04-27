using Microsoft.Extensions.Azure;
using Nhs.Appointments.Core.Blob;
using Nhs.Appointments.Core.Concurrency;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInMemoryLeasing(this IServiceCollection services)
    {
        services.Configure<LeaseManagerOptions>(opts => opts.Timeout = TimeSpan.FromSeconds(15));
        return services.AddSingleton<ILeaseManager, InMemoryLeaseManager>();
    }

    public static IServiceCollection AddAzureBlobStoreLeasing(this IServiceCollection services, string connectionString, string containerName)
    {
        services.Configure<LeaseManagerOptions>(opts => { 
            opts.Timeout = TimeSpan.FromSeconds(30);
            opts.ContainerName = containerName;
        });
                    
        services.AddAzureClients(x =>
        {
            x.AddBlobServiceClient(connectionString);
        });

        return services
            .AddSingleton<IAzureBlobStorage, AzureBlobStorage>()
            .AddSingleton<ILeaseManager, AzureStorageLeaseManager>();
    }
}
