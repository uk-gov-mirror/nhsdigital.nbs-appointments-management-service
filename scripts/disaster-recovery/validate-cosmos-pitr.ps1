param (
    [Parameter(Mandatory=$true)][string]$sourceAccount,
    [Parameter(Mandatory=$true)][string]$resourceGroup,
    [Parameter(Mandatory=$true)][string]$restoreDateAndTimeStr
)

Write-Host "--- Starting PITR Validation ---"

# Fetch Account Tier (Continuous7Days or Continuous30Days)
$policy = az cosmosdb show -n $sourceAccount -g $resourceGroup --query "backupPolicy.continuousModeProperties.tier" -o tsv

if ($null -eq $policy) {
    Write-Error "Could not retrieve backup policy for $sourceAccount. Ensure the account exists and has continuous backup enabled."
    exit 1
}

$retentionDays = if ($policy -eq "Continuous30Days") { 30 } else { 7 }
Write-Host "Account Retention Tier: $retentionDays days."

# Parse and Convert from UK Local to UTC
try {
    $localTime = [DateTime]::Parse($restoreDateAndTimeStr)
    $ukTimeZone = [TimeZoneInfo]::FindSystemTimeZoneById("Europe/London") 
    $restoreTime = [TimeZoneInfo]::ConvertTimeToUtc($localTime, $ukTimeZone)
    
    Write-Host "Detected UK Input:  $($localTime.ToString('u'))"
    Write-Host "Converted for Azure (UTC): $($restoreTime.ToString('u'))"
} catch {
    Write-Error "Invalid Date Format: $restoreDateAndTimeStr. Please use YYYY-MM-DD HH:mm:ss (UK Local Time)."
    exit 1
}

$now = [DateTime]::UtcNow
$ageInDays = ($now - $restoreTime).TotalDays

Write-Host "Calculated Data Age:    $($ageInDays.ToString('F2')) days"

# Logic Validation
if ($ageInDays -lt 0) {
    Write-Host "##vso[task.logissue type=error]Restore timestamp cannot be in the future."
    exit 1
}

if ($ageInDays -gt $retentionDays) {
    Write-Host "##vso[task.logissue type=error]Selected date is $($ageInDays.ToString('F1')) days old, exceeding the $retentionDays-day limit."
    exit 1
}

$azureFormatDate = $restoreTime.ToString("yyyy-MM-dd HH:mm:ss +00:00")
Write-Host "##vso[task.setvariable variable=ConvertedUtcTime;isOutput=true]$azureFormatDate"
Write-Host "Validation Successful. Output set: $azureFormatDate"