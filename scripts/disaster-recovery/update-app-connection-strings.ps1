param (
    [Parameter(Mandatory=$true)][string]$resourceGroup,
    [Parameter(Mandatory=$true)][string]$environment,
    [Parameter(Mandatory=$true)][string]$region,
    [Parameter(Mandatory=$true)][string]$targetAccountName # This is the "Secondary" account name
)

Write-Host "--- Updating Connection Strings to $targetAccountName ---"

# Fetch the New Connection String from the SECONDARY account
$connectionString = az cosmosdb keys list `
    --name $targetAccountName `
    --resource-group $resourceGroup `
    --type connection-strings `
    --query "connectionStrings[0].connectionString" `
    -o tsv

if (-not $connectionString) {
    Write-Error "Failed to retrieve connection string for $targetAccountName"
    exit 1
}

# Update Key Vault (The Shared Source of Truth)
$vaultName = "nbs-mya-kv-$environment-$region" 

# NOTE: Use the secret name Terraform expects (usually double-dash or single-dash)
$secretName = "CosmosDb--ConnectionString" 

Write-Host "Updating Key Vault: $vaultName. This prevents redeploys from reverting to the old DB."
az keyvault secret set `
    --vault-name $vaultName `
    --name $secretName `
    --value $connectionString `
    --description "PITR Restore: Pointing to $targetAccountName"

# Define target apps (Matching Stephen's list)
$webApp = "nbs-mya-app-$environment-$region"
$functionApps = @(
    "nbs-mya-func-$environment-$region", 
    "nbs-mya-hlfunc-$environment-$region", 
    "nbs-mya-sbfunc-$environment-$region", 
    "nbs-mya-timerfunc-$environment-$region"
)

# Update Web App Settings (Direct update for immediate effect)
Write-Host "Updating Web App: $webApp"
az webapp config appsettings set -g $resourceGroup -n $webApp --settings "CosmosDb__ConnectionString=$connectionString"

# Update Function App Settings
foreach ($app in $functionApps) {
    Write-Host "Updating Function App: $app"
    az functionapp config appsettings set -g $resourceGroup -n $app --settings "CosmosDb__ConnectionString=$connectionString"
}

Write-Host "--- Configuration Update Complete. System is now using $targetAccountName ---"