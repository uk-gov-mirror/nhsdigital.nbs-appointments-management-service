param (
    [Parameter(Mandatory=$true)][string]$sourceAccount,
    [Parameter(Mandatory=$true)][string]$resourceGroup,
    [Parameter(Mandatory=$true)][string]$restoreDateAndTimeStr
)

Write-Host "--- Starting PITR Validation ---"

# 1. Fetch Account Tier (Continuous7Days or Continuous30Days)
$policy = az cosmosdb show -n $sourceAccount -g $resourceGroup --query "backupPolicy.continuousModeProperties.tier" -o tsv

if ($null -eq $policy) {
    Write-Error "Could not retrieve backup policy for $sourceAccount. Ensure the account exists and has continuous backup enabled."
    exit 1
}

$retentionDays = if ($policy -eq "Continuous30Days") { 30 } else { 7 }
Write-Host "Account Retention Tier: $retentionDays days."

# 2. Parse and Compare Dates
try {
    $restoreTime = [DateTime]::Parse($restoreDateAndTimeStr).ToUniversalTime()
} catch {
    Write-Error "Invalid Date Format: $restoreDateAndTimeStr. Please use ISO 8601 (YYYY-MM-DDTHH:mm:ssZ)."
    exit 1
}

$now = [DateTime]::UtcNow
$ageInDays = ($now - $restoreTime).TotalDays

Write-Host "Current UTC Time:       $($now.ToString('u'))"
Write-Host "Requested Restore Time: $($restoreTime.ToString('u'))"
Write-Host "Calculated Data Age:    $($ageInDays.ToString('F2')) days"

# 3. Logic Validation
if ($ageInDays -lt 0) {
    Write-Host "##vso[task.logissue type=error]Restore timestamp cannot be in the future."
    exit 1
}

if ($ageInDays -gt $retentionDays) {
    Write-Host "##vso[task.logissue type=error]Selected date is $($ageInDays.ToString('F1')) days old, exceeding the $retentionDays-day limit."
    exit 1
}

Write-Host "Validation Successful. The requested point-in-time is within the recovery window."