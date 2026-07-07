param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$DotnetPath,
    [string]$GameDir,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.ps1')

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resolvedDotnet = Resolve-DotnetExecutable $DotnetPath
if (-not (Test-Dotnet8Sdk $resolvedDotnet)) {
    throw '.NET 8 SDK is required. Install it or pass -DotnetPath to dotnet.exe.'
}

$resolvedGame = Resolve-ONIGameDirectory $GameDir
if ([string]::IsNullOrWhiteSpace($resolvedGame)) {
    throw 'Oxygen Not Included was not found. Pass -GameDir or set ONI_GAME_DIR.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'ONITalk-build'
$env:DOTNET_CLI_HOME = Join-Path $tempRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $tempRoot 'nuget-packages'
$env:APPDATA = Join-Path $tempRoot 'appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null

$nugetConfig = Join-Path $repositoryRoot 'NuGet.Config'
$testsProject = Join-Path $repositoryRoot 'src\ONITalk.Core.Tests\ONITalk.Core.Tests.csproj'
$modProject = Join-Path $repositoryRoot 'src\ONITalk.Mod\ONITalk.Mod.csproj'

Write-Host ("Using .NET: {0}" -f $resolvedDotnet)
Write-Host ("Using ONI:  {0}" -f $resolvedGame)

& (Join-Path $PSScriptRoot 'check-localization.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipTests) {
    & $resolvedDotnet restore $testsProject --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $resolvedDotnet run --project $testsProject --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $resolvedDotnet restore $modProject --configfile $nugetConfig -p:GameDir=$resolvedGame
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $resolvedDotnet build $modProject --no-restore --configuration $Configuration -p:GameDir=$resolvedGame
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$artifact = Join-Path $repositoryRoot 'artifacts\ONITalk\ONITalk.dll'
if (-not (Test-Path -LiteralPath $artifact)) {
    throw ("Build completed but artifact was not found: {0}" -f $artifact)
}

Write-Host ("Build complete: {0}" -f $artifact)
