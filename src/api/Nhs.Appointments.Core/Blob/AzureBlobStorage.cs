using Azure.Storage.Blobs;

namespace Nhs.Appointments.Core.Blob;

public class AzureBlobStorage(BlobServiceClient blobServiceClient) : IAzureBlobStorage
{
    public async Task<Stream> GetBlobUploadStream(string containerName, string blobName)
    {
        var blobClient = await GetBlobClientFromContainerAndBlobNameAsync(containerName, blobName);

        return await blobClient.OpenWriteAsync(true);
    }

    public BlobClient GetBlobClientFromContainerAndBlobName(string containerName, string blobName)
    {
        var containerClient = ResolveContainerClient(containerName);

        return containerClient.GetBlobClient(blobName);
    }

    public async Task<BlobClient> GetBlobClientFromContainerAndBlobNameAsync(string containerName, string blobName)
    {
        var containerClient = await ResolveContainerClientAsync(containerName);
        
        return containerClient.GetBlobClient(blobName);
    }

    private BlobContainerClient ResolveContainerClient(string containerName)
    {
        var containers = blobServiceClient.GetBlobContainers();
        
        return containers.Any(x => x.Name.Equals(containerName)) 
            ? blobServiceClient.GetBlobContainerClient(containerName) 
            : blobServiceClient.CreateBlobContainer(containerName);
    }
    
    private async Task<BlobContainerClient> ResolveContainerClientAsync(string containerName)
    {
        var exists = await blobServiceClient.GetBlobContainersAsync().AnyAsync(x => x.Name.Equals(containerName));
        
        return exists
            ? blobServiceClient.GetBlobContainerClient(containerName) 
            : await blobServiceClient.CreateBlobContainerAsync(containerName);
    }
}
