param (
    [Parameter(Mandatory=$true)][string]$vaultName,
    [Parameter(Mandatory=$true)][string]$resourceGroup,
    [Parameter(Mandatory=$true)][string]$cosmosAccountName
)

$ErrorActionPreference = "Stop"

function Check-LastCommand {
    param([string]$ErrorMessage)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "##vso[task.logissue type=error]$ErrorMessage"
        throw $ErrorMessage
    }
}

Write-Host "--- Updating Configuration for Restored Cosmos DB ---"
Write-Host "Target Account: $cosmosAccountName"
Write-Host "Target Vault:   $vaultName"

# Fetch the Endpoint URL
Write-Host "Fetching document endpoint..."
$endpoint = az cosmosdb show `
    --name $cosmosAccountName `
    --resource-group $resourceGroup `
    --query documentEndpoint -o tsv
Check-LastCommand "Failed to fetch Cosmos DB endpoint for $cosmosAccountName"

# Fetch the Primary Key (Token)
Write-Host "Fetching primary master key..."
$primaryKey = az cosmosdb keys list `
    --name $cosmosAccountName `
    --resource-group $resourceGroup `
    --query primaryMasterKey -o tsv
Check-LastCommand "Failed to fetch Cosmos DB keys for $cosmosAccountName"

# Update Key Vault Secrets
$secrets = @{
    "ACTIVE-COSMOS-DB-ACCOUNT"  = $cosmosAccountName
    "COSMOS-ENDPOINT"           = $endpoint
    "COSMOS-TOKEN"              = $primaryKey
}

foreach ($secret in $secrets.GetEnumerator()) {
    Write-Host "Updating secret: $($secret.Key)..."
    az keyvault secret set `
        --vault-name $vaultName `
        --name $secret.Key `
        --value $secret.Value `
        --description "Updated by PITR Pipeline on $(Get-Date)" `
        > $null
    
    Check-LastCommand "Failed to update Key Vault secret: $($secret.Key)"
}

Write-Host "--- Configuration Update Complete ---"