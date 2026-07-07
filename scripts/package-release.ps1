param(
    [string]$GameDir,
    [string]$DotnetPath,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactRoot = Join-Path $repositoryRoot 'artifacts\ONITalk'
$releaseRoot = Join-Path $repositoryRoot 'artifacts\releases'

if (-not $SkipBuild) {
    $buildArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
        (Join-Path $PSScriptRoot 'build.ps1'))
    if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
        $buildArguments += @('-GameDir', $GameDir)
    }
    if (-not [string]::IsNullOrWhiteSpace($DotnetPath)) {
        $buildArguments += @('-DotnetPath', $DotnetPath)
    }
    & powershell.exe @buildArguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$manifestPath = Join-Path $artifactRoot 'mod_info.yaml'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Release manifest was not found: $manifestPath"
}
$versionLine = Get-Content -Encoding UTF8 -LiteralPath $manifestPath |
    Where-Object { $_ -match '^version:\s*(?<version>[^\s]+)\s*$' } |
    Select-Object -First 1
if ($null -eq $versionLine) {
    throw 'mod_info.yaml does not declare a version.'
}
$version = [regex]::Match($versionLine,
    '^version:\s*(?<version>[^\s]+)\s*$').Groups['version'].Value

$allowedFiles = @(
    'config.example.json',
    'mod.yaml',
    'mod_info.yaml',
    'ONITalk.dll',
    'ONITalk-LICENSE.txt',
    'PLib-LICENSE.txt',
    'PLib-NOTICE.md',
    'translations/es.po',
    'translations/template.pot',
    'translations/zh.po'
)
$actualFiles = @(Get-ChildItem -LiteralPath $artifactRoot -Recurse -File |
    ForEach-Object {
        $_.FullName.Substring($artifactRoot.Length + 1).Replace('\', '/')
    } | Sort-Object)
$expectedFiles = @($allowedFiles | Sort-Object)
if (($actualFiles -join "`n") -ne ($expectedFiles -join "`n")) {
    throw ("Artifact contents differ from the release allowlist.`nActual:`n{0}" -f
        ($actualFiles -join "`n"))
}

foreach ($relative in $allowedFiles) {
    $path = Join-Path $artifactRoot $relative.Replace('/', '\')
    if ([System.IO.Path]::GetExtension($path) -in @('.json', '.yaml', '.txt', '.md',
            '.po', '.pot')) {
        $text = Get-Content -Encoding UTF8 -Raw -LiteralPath $path
        if ($text -match '"apiKey"\s*:\s*"(?!")') {
            throw "A non-empty API key was found in release input: $relative"
        }
        if ($text -match '(?i)\bsk-[a-z0-9_-]{12,}') {
            throw "A possible secret was found in release input: $relative"
        }
    }
}

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
$zipName = "ONITalk-v$version.zip"
$zipPath = Join-Path $releaseRoot $zipName
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
try {
    $archive = [System.IO.Compression.ZipArchive]::new($stream,
        [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        $fixedTimestamp = [DateTimeOffset]::new(2020, 1, 1, 0, 0, 0,
            [TimeSpan]::Zero)
        foreach ($relative in $allowedFiles | Sort-Object) {
            $source = Join-Path $artifactRoot $relative.Replace('/', '\')
            $entry = $archive.CreateEntry("ONITalk/$relative",
                [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $fixedTimestamp
            $input = [System.IO.File]::OpenRead($source)
            $output = $entry.Open()
            try {
                $input.CopyTo($output)
            } finally {
                $output.Dispose()
                $input.Dispose()
            }
        }
    } finally {
        $archive.Dispose()
    }
} finally {
    $stream.Dispose()
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = Join-Path $releaseRoot "ONITalk-v$version.sha256"
Set-Content -Encoding ASCII -LiteralPath $hashPath -Value "$hash  $zipName"

Write-Host "Release package: $zipPath"
Write-Host "SHA-256:        $hash"
