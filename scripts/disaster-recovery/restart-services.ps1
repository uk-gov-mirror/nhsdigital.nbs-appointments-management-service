param (
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup
)

# Start the Web App
$webApp = "vaccs-mya-app-dev-uks"
Write-Host "Starting Web App: $webApp in $ResourceGroup..."
az webapp start --name $webApp --resource-group $ResourceGroup

# Start the Function Apps (Excluding hlfunc)
$functions = @(
    "vaccs-mya-func-dev-uks",
    "vaccs-mya-sbfunc-dev-uks",
    "vaccs-mya-timerfunc-dev-uks"
)

foreach ($app in $functions) {
    Write-Host "Starting Function App: $app in $ResourceGroup..."
    az functionapp start --name $app --resource-group $ResourceGroup
}