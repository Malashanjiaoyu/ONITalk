param(
    [string]$UserDataDir
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.ps1')

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactDir = Join-Path $repositoryRoot 'artifacts\ONITalk'
if (-not (Test-Path -LiteralPath (Join-Path $artifactDir 'ONITalk.dll'))) {
    throw 'Build artifact is missing. Run scripts\build.ps1 first.'
}

if ([string]::IsNullOrWhiteSpace($UserDataDir)) {
    $UserDataDir = Resolve-ONIUserDataDirectory
}
if ([string]::IsNullOrWhiteSpace($UserDataDir) -or
        -not (Test-Path -LiteralPath $UserDataDir)) {
    throw 'ONI user data directory was not found. Pass -UserDataDir explicitly.'
}

$destination = Join-Path $UserDataDir 'mods\Local\ONITalk'
New-Item -ItemType Directory -Path $destination -Force | Out-Null

$files = @(
    'ONITalk.dll',
    'mod.yaml',
    'mod_info.yaml',
    'config.example.json',
    'PLib-LICENSE.txt',
    'PLib-NOTICE.md'
)
foreach ($name in $files) {
    $source = Join-Path $artifactDir $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Artifact is missing: {0}" -f $source)
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name) -Force
}

$translationSource = Join-Path $artifactDir 'translations'
$translationDestination = Join-Path $destination 'translations'
if (-not (Test-Path -LiteralPath $translationSource)) {
    throw ("Artifact is missing: {0}" -f $translationSource)
}
New-Item -ItemType Directory -Path $translationDestination -Force | Out-Null
$translationFiles = Get-ChildItem -LiteralPath $translationSource -File
foreach ($file in $translationFiles) {
    $destinationPath = Join-Path $translationDestination $file.Name
    Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force
}

foreach ($name in $files) {
    $sourceHash = (Get-FileHash -LiteralPath (Join-Path $artifactDir $name) -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath (Join-Path $destination $name) -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) {
        throw ("Installed file verification failed: {0}" -f $name)
    }
}

foreach ($file in $translationFiles) {
    $destinationPath = Join-Path $translationDestination $file.Name
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) {
        throw ("Installed translation verification failed: {0}" -f $file.Name)
    }
}

Write-Host ("Installed and verified: {0}" -f $destination)
