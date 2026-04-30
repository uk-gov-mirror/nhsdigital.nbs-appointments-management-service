using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Blob;
using Polly;

namespace Nhs.Appointments.Core.Concurrency;

internal class AzureStorageLeaseManager : ILeaseManager
{
    private readonly IAzureBlobStorage _azureBlobStorage;
    private readonly LeaseManagerOptions _defaultOptions;
    private readonly int _delayRetryTimeInMilliseconds;

    public AzureStorageLeaseManager(
        IOptions<LeaseManagerOptions> options, 
        IAzureBlobStorage azureBlobStorage, 
        int delayRetryTimeInMilliseconds = 100
        )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(delayRetryTimeInMilliseconds, 0, nameof(delayRetryTimeInMilliseconds));

        _azureBlobStorage = azureBlobStorage ?? throw new ArgumentNullException(nameof(azureBlobStorage));
        _delayRetryTimeInMilliseconds = delayRetryTimeInMilliseconds;
        _defaultOptions = options.Value;
    }

    public string Mode => LeaseManagerMode.DistributedAzureBlob;

    public ILeaseContext Acquire(string leaseKey, LeaseManagerOptions options = null)
    {
        var leaseClient = GetLeaseClient(ResolveContainerName(options),leaseKey);
        CreateResiliencePipeline().Execute(() => leaseClient.Acquire(ResolveTimeout(options)));

        return new LeaseContext(leaseKey, () => leaseClient.Release());
    }
    
    public async Task<ILeaseContext> AcquireAsync(string leaseKey, LeaseManagerOptions options = null)
    {
        var leaseClient = await GetLeaseClientAsync(ResolveContainerName(options),leaseKey);
        await CreateResiliencePipeline().ExecuteAsync(
            async (cancellationToken) => await leaseClient.AcquireAsync(
                ResolveTimeout(options), 
                cancellationToken: cancellationToken));

        return new LeaseContext(leaseKey, () => leaseClient.Release());
    }

    private BlobLeaseClient GetLeaseClient(string containerName, string blobName)
    {
        var blobClient = _azureBlobStorage.GetBlobClientFromContainerAndBlobName(containerName, blobName);
        if (blobClient.Exists() == false)
        {
            blobClient.Upload(BinaryData.FromString(""));
        }

        return blobClient.GetBlobLeaseClient();
    }
    
    private async Task<BlobLeaseClient> GetLeaseClientAsync(string containerName, string blobName)
    {
        var blobClient = await _azureBlobStorage.GetBlobClientFromContainerAndBlobNameAsync(containerName, blobName);
        if (await blobClient.ExistsAsync() == false)
        {
            await blobClient.UploadAsync(BinaryData.FromString(""));
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

    private string ResolveContainerName(LeaseManagerOptions options = null) => options?.Realm ?? _defaultOptions.Realm;
    private TimeSpan ResolveTimeout(LeaseManagerOptions options = null) =>
        options?.Timeout ?? _defaultOptions.Timeout;
}
