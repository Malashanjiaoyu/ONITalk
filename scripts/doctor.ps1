param(
    [string]$DotnetPath,
    [string]$GameDir
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.ps1')

$failures = 0
function Write-Check {
    param(
        [string]$Status,
        [string]$Name,
        [string]$Detail
    )
    Write-Host ("[{0}] {1}: {2}" -f $Status, $Name, $Detail)
}

$resolvedDotnet = Resolve-DotnetExecutable $DotnetPath
if (Test-Dotnet8Sdk $resolvedDotnet) {
    $versions = (Get-DotnetSdkVersions $resolvedDotnet) -join ', '
    Write-Check 'OK' '.NET 8 SDK' ($resolvedDotnet + ' | ' + $versions)
} else {
    $failures++
    if ([string]::IsNullOrWhiteSpace($resolvedDotnet)) {
        Write-Check 'MISSING' '.NET 8 SDK' 'dotnet was not found'
    } else {
        Write-Check 'MISSING' '.NET 8 SDK' ($resolvedDotnet + ' has no .NET 8 SDK')
    }
}

$resolvedGame = Resolve-ONIGameDirectory $GameDir
if ([string]::IsNullOrWhiteSpace($resolvedGame)) {
    $failures++
    Write-Check 'MISSING' 'Oxygen Not Included' 'game directory or Assembly-CSharp.dll was not found'
} else {
    Write-Check 'OK' 'Oxygen Not Included' $resolvedGame
    $managed = Join-Path $resolvedGame 'OxygenNotIncluded_Data\Managed'
    foreach ($name in @(
        'Assembly-CSharp.dll',
        'Assembly-CSharp-firstpass.dll',
        '0Harmony.dll',
        'Newtonsoft.Json.dll',
        'UnityEngine.dll',
        'UnityEngine.CoreModule.dll'
    )) {
        $path = Join-Path $managed $name
        if (Test-Path -LiteralPath $path) {
            Write-Check 'OK' $name $path
        } else {
            $failures++
            Write-Check 'MISSING' $name $path
        }
    }
}

$userData = Resolve-ONIUserDataDirectory
if ([string]::IsNullOrWhiteSpace($userData)) {
    Write-Check 'INFO' 'ONI user data' 'not found; the game may not have created it yet'
} else {
    Write-Check 'OK' 'ONI user data' $userData
}

$git = Resolve-GitExecutable
if ([string]::IsNullOrWhiteSpace($git)) {
    Write-Check 'OPTIONAL' 'Git' 'not installed; recommended for version control, not required to build'
} else {
    $gitVersion = (& $git --version 2>$null) -join ' '
    Write-Check 'OK' 'Git' ($git + ' | ' + $gitVersion)
}

if ($failures -gt 0) {
    Write-Host ("Environment check failed with {0} required item(s) missing." -f $failures)
    exit 1
}

Write-Host 'Environment check passed.'
