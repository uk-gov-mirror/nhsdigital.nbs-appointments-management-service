param (
    [Parameter(Mandatory=$true)][string]$sourceAccount,
    [Parameter(Mandatory=$true)][string]$resourceGroup
)

$ErrorActionPreference = "Stop"

Write-Host "--- APPT-2312: Taking Corrupted DB Offline ---"
Write-Host "Target: $sourceAccount"

# Disabling public network access ensures any misconfigured app fails fast
az cosmosdb update `
    --name $sourceAccount `
    --resource-group $resourceGroup `
    --public-network-access Disabled

Write-Host "##[section]Source DB is now offline. Connections to this resource will now fail by design."