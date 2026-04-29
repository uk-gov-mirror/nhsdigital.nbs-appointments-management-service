param (
  [string][Parameter(Mandatory)]$resourceGroup,
  [string][Parameter(Mandatory)]$region
)

# Start the Web App
$webApp = "vaccs-mya-app-dev-$region"
Write-Host "Starting Web App: $webApp in $ResourceGroup..."
az webapp start --name $webApp --resource-group $ResourceGroup

# Start the Function Apps (Excluding hlfunc)
$functions = @(
    "vaccs-mya-func-dev-$region",
    "vaccs-mya-sbfunc-dev-$region",
    "vaccs-mya-timerfunc-dev-$region"
)

foreach ($app in $functions) {
    Write-Host "Starting Function App: $app in $ResourceGroup..."
    az functionapp start --name $app --resource-group $ResourceGroup
}

$containerApps = @("vaccs-mya-aggregator-dev-$region", "vaccs-mya-auditor-dev-$region")

foreach ($containerApp in $containerApps) {
  Write-Host "Restarting inactive revisions in container app '$containerApp'"

  $revisions = @(az containerapp revision list `
    --name $containerApp `
    --resource-group $resourceGroup `
    --query "[?!properties.active].name" `
    -o tsv)

  if (-not $revisions) {
    Write-Host "No inactive revisions found for $containerApp"
    continue
  }

  foreach ($rev in $revisions) {
    Write-Host "Reactivating revision '$rev'"

    az containerapp revision activate `
      --name $containerApp `
      --resource-group $resourceGroup `
      --revision $rev

    if ($LASTEXITCODE -ne 0) {
      Write-Warning "Failed to activate revision '$rev' for '$containerApp'"
      $LASTEXITCODE = 0
    }
  }
}