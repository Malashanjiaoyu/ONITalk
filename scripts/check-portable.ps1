param([string]$DotnetPath)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.ps1')

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dotnet = Resolve-DotnetExecutable $DotnetPath
if (-not (Test-Dotnet8Sdk $dotnet)) {
    throw '.NET 8 SDK is required.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'ONITalk-portable-checks'
$env:DOTNET_CLI_HOME = Join-Path $tempRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $tempRoot 'nuget-packages'
$env:APPDATA = Join-Path $tempRoot 'appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null

& (Join-Path $PSScriptRoot 'check-localization.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'check-secrets.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'check-workshop.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Join-Path $root 'src\ONITalk.Core.Tests\ONITalk.Core.Tests.csproj'
$nugetConfig = Join-Path $root 'NuGet.Config'
& $dotnet restore $project --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $dotnet run --project $project --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'All portable repository checks passed.'
