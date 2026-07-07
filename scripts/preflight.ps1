param(
    [string]$UserDataDir
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.ps1')
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$UserDataDir = if ([string]::IsNullOrWhiteSpace($UserDataDir)) {
    Resolve-ONIUserDataDirectory
} else {
    $UserDataDir
}
$reportPath = Join-Path $root 'artifacts\preflight-report.txt'
$lines = [System.Collections.Generic.List[string]]::new()
$failures = 0

function Add-Check([string]$status, [string]$name, [string]$detail) {
    $script:lines.Add("[$status] $name - $detail")
    if ($status -eq 'FAIL') { $script:failures++ }
}

$manifestPath = Join-Path $root 'src\ONITalk.Mod\mod_info.yaml'
$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath
if ($manifest -match 'supportedContent:\s*VANILLA_ID,EXPANSION1_ID') {
    Add-Check 'PASS' 'Vanilla and Spaced Out manifest' 'both content IDs are declared'
} else {
    Add-Check 'FAIL' 'Vanilla and Spaced Out manifest' 'supportedContent is incomplete'
}

$configPath = if ([string]::IsNullOrWhiteSpace($UserDataDir)) { $null } else {
    Join-Path $UserDataDir 'mods\config\ONITalk\ONITalk.json'
}
if ($null -ne $configPath -and (Test-Path -LiteralPath $configPath)) {
    $config = Get-Content -Raw -Encoding UTF8 -LiteralPath $configPath | ConvertFrom-Json
    $schema = if ($null -eq $config.schemaVersion) { 1 } else { [int]$config.schemaVersion }
    Add-Check 'PASS' 'Config migration input' "schema $schema is readable; target is schema 4"
} else {
    Add-Check 'INFO' 'Config migration input' 'no existing config; clean install path applies'
}

$memoryDir = if ([string]::IsNullOrWhiteSpace($UserDataDir)) { $null } else {
    Join-Path $UserDataDir 'mods\config\ONITalk\memory'
}
$ids = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$memoryFiles = @()
if ($null -ne $memoryDir -and (Test-Path -LiteralPath $memoryDir)) {
    $memoryFiles = @(Get-ChildItem -LiteralPath $memoryDir -Filter '*.json' -File)
}
foreach ($file in $memoryFiles) {
    try {
        $document = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName |
            ConvertFrom-Json
        $id = [string]$document.colonyId
        $schema = [int]$document.schemaVersion
        if ([string]::IsNullOrWhiteSpace($id) -or $file.BaseName -ne $id) {
            Add-Check 'FAIL' 'Colony memory identity' "$($file.Name) does not match colonyId"
        } elseif ($schema -notin @(1, 2, 3)) {
            Add-Check 'FAIL' 'Colony memory schema' "$($file.Name) uses unsupported schema $schema"
        } elseif (-not $ids.Add($id)) {
            Add-Check 'FAIL' 'Colony memory isolation' "duplicate colonyId $id"
        } else {
            Add-Check 'PASS' 'Colony memory file' "$($file.Name), schema $schema"
        }
    } catch {
        Add-Check 'FAIL' 'Colony memory parse' "$($file.Name): $($_.Exception.Message)"
    }
}
if ($memoryFiles.Count -ge 2 -and $ids.Count -eq $memoryFiles.Count) {
    Add-Check 'PASS' 'Real cross-save isolation' "$($ids.Count) distinct colony files verified"
} elseif ($memoryFiles.Count -lt 2) {
    Add-Check 'INFO' 'Real cross-save isolation' 'a second colony is still needed for live proof'
}

$servicePath = Join-Path $root 'src\ONITalk.Mod\Runtime\ONITalkService.cs'
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath $servicePath
if ($source -match 'RemoteFailureBackoff' -and
        $source -match 'using offline fallback') {
    Add-Check 'PASS' 'Remote failure fallback' 'offline fallback and bounded retry backoff are wired'
} else {
    Add-Check 'FAIL' 'Remote failure fallback' 'fallback/backoff wiring was not found'
}

$lines.Add("[INFO] GeneratedUtc - $([DateTimeOffset]::UtcNow.ToString('O'))")
New-Item -ItemType Directory -Path (Split-Path $reportPath) -Force | Out-Null
Set-Content -Encoding UTF8 -LiteralPath $reportPath -Value $lines
$lines | ForEach-Object { Write-Host $_ }
Write-Host "Preflight report: $reportPath"
if ($failures -gt 0) { exit 1 }
