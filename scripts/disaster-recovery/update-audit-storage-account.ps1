param (
    [string][Parameter(Mandatory)]$resourceGroup,
    [string][Parameter(Mandatory)]$blobAccountName,
    [string][Parameter(Mandatory)]$containerApp
)

Write-Host "Fetching storage account connection string..."

$connectionString = az storage account show-connection-string --name $blobAccountName --resource-group $resourceGroup --query connectionString -o tsv

if (-not $connectionString) {
    throw "Failed to retrieve connection string for $blobAccountName"
}

Write-Host "Updating Container App secret..."

az containerapp secret set --name $containerApp --resource-group $resourceGroup --secrets storageconn="$connectionString"

if ($LASTEXITCODE -ne 0) {
    throw "Failed to update secret for Container App '$containerApp'"
}

Write-Host "Updating Container App environment variable..."

az containerapp update --name $containerApp --resource-group $resourceGroup --set-env-vars BLOB_STORAGE_CONNECTION_STRING=secretref:storageconn

if ($LASTEXITCODE -ne 0) {
    throw "Failed to update environment variable for Container App '$containerApp'"
}