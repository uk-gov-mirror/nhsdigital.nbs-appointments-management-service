# This is for testing the PITR on the dev environment, it will be removed and the existing stop-app-services script will be used for the PITR process.

param (
  [string][Parameter(Mandatory)]$resourceGroup,
  [string][Parameter(Mandatory)]$region
)

$ErrorActionPreference = "Stop"
$DebugPreference = "Continue"

$webAppService = "vaccs-mya-app-dev-$region"
$functionAppServices = @("vaccs-mya-func-dev-$region", "vaccs-mya-sbfunc-dev-$region", "vaccs-mya-timerfunc-dev-$region")
$containerApps = @("vaccs-mya-aggregator-dev-$region", "vaccs-mya-auditor-dev-$region")

foreach ($functionApp in $functionAppServices) {
  Write-Host "Stopping function app '$functionApp' in resource group '$resourceGroup'"
  az functionapp stop `
    --resource-group $resourceGroup `
    --name $functionApp
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to stop function app '$functionApp' in resource group '$resourceGroup'. The DR process will continue anyway."
    $LASTEXITCODE = 0
  }
}

Write-Host "Stopping web app '$webAppService' in resource group '$resourceGroup'"
az webapp stop `
  --name $webAppService `
  --resource-group $resourceGroup
if ($LASTEXITCODE -ne 0) {
  Write-Warning "Failed to stop Web App: $webAppService. The DR process will continue anyway."
  $LASTEXITCODE = 0
}

# Setting the min and max replicas to 1 to effectively stop the container apps, as there is no direct stop command for container apps in Azure CLI
foreach ($containerApp in $containerApps) {
  Write-Host "Stopping container app '$containerApp'"

  az containerapp update `
    --name $containerApp `
    --resource-group $resourceGroup `
    --min-replicas 0 `
    --max-replicas 1

  if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to stop Container App: $containerApp."
    $LASTEXITCODE = 0
  }
}

exit 0
