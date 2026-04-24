# This is for testing the PITR on the dev environment, it will be removed and the existing stop-app-services script will be used for the PITR process.

param (
  [string][Parameter(Mandatory)]$resourceGroup,
  [string][Parameter(Mandatory)]$region,
  [string][Parameter(Mandatory)]$subscriptionId
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

foreach ($containerApp in $containerApps) {
  Stop-AzContainerApp -Name $containerApp -ResourceGroupName $resourceGroup -SubscriptionId 85913fdb-7f11-4963-8519-70879ffd2910
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to stop Container App: $containerApp."
    $LASTEXITCODE = 0
  }
}

exit 0
