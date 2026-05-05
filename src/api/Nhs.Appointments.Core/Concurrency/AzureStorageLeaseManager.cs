using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Blob;
using Polly;

namespace Nhs.Appointments.Core.Concurrency;

internal class AzureStorageLeaseManager : ILeaseManager
{
    private readonly IAzureBlobStorage _azureBlobStorage;
    private readonly LeaseManagerOptions _options;
    private readonly int _acquireTimeInSeconds;
    private readonly int _delayRetryTimeInMilliseconds;

    public AzureStorageLeaseManager(
        IOptions<LeaseManagerOptions> options, 
        IAzureBlobStorage azureBlobStorage, 
        int acquireTimeInSeconds = 20, 
        int delayRetryTimeInMilliseconds = 100
        )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(acquireTimeInSeconds, 0, nameof(acquireTimeInSeconds));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(delayRetryTimeInMilliseconds, 0, nameof(delayRetryTimeInMilliseconds));

        _azureBlobStorage = azureBlobStorage ?? throw new ArgumentNullException(nameof(azureBlobStorage));
        _options = options.Value;
        _acquireTimeInSeconds = acquireTimeInSeconds;
        _delayRetryTimeInMilliseconds = delayRetryTimeInMilliseconds;
    }

    public ILeaseContext Acquire(string leaseKey)
    {
        var leaseClient = GetLeaseClient(leaseKey);
        var leasePipeline = CreateResiliencePipeline();
        leasePipeline.Execute(() => leaseClient.Acquire(TimeSpan.FromSeconds(_acquireTimeInSeconds)));

        return new LeaseContext(leaseKey, () => leaseClient.Release());
    }

    private BlobLeaseClient GetLeaseClient(string blobName)
    {
        var blobClient = _azureBlobStorage.GetBlobClientFromContainerAndBlobName(_options.ContainerName, blobName);
        if (blobClient.Exists() == false)
        {
            blobClient.Upload(BinaryData.FromString(""));
        }

        return blobClient.GetBlobLeaseClient();
    }

    private ResiliencePipeline<Azure.Response<BlobLease>> CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder<Azure.Response<BlobLease>>()
            .AddRetry(new Polly.Retry.RetryStrategyOptions<Azure.Response<BlobLease>>
            {
                ShouldHandle = arguments => arguments.Outcome switch
                {
                    { Exception: Azure.RequestFailedException ex } when ex.ErrorCode == "LeaseAlreadyPresent" => PredicateResult.True(),
                    _ => PredicateResult.False(),
                },
                MaxRetryAttempts = 20,
                Delay = TimeSpan.FromMilliseconds(_delayRetryTimeInMilliseconds)
            })
            .Build();
    }                
}
