$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$workshop = Join-Path $root 'workshop'
$manifest = Join-Path $root 'src\ONITalk.Mod\mod_info.yaml'
$metadata = Join-Path $workshop 'metadata.yml'
$required = @(
    'metadata.yml',
    'description_zh.bbcode',
    'description_en.bbcode',
    'description_es.bbcode',
    'SCREENSHOT_PLAN.md',
    'ASSET_BRIEF.md',
    'UPLOAD_CHECKLIST.md'
)

foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $workshop $name))) {
        throw "Workshop file is missing: $name"
    }
}

$manifestVersion = [regex]::Match(
    (Get-Content -Raw -Encoding UTF8 -LiteralPath $manifest),
    '(?m)^version:\s*(?<value>[^\s]+)\s*$').Groups['value'].Value
$metadataVersion = [regex]::Match(
    (Get-Content -Raw -Encoding UTF8 -LiteralPath $metadata),
    '(?m)^version:\s*"(?<value>[^"]+)"\s*$').Groups['value'].Value
if ([string]::IsNullOrWhiteSpace($manifestVersion) -or
        $manifestVersion -ne $metadataVersion) {
    throw "Workshop version '$metadataVersion' does not match mod '$manifestVersion'."
}

$tags = @('h1', 'h2', 'b', 'quote', 'list', 'url')
foreach ($file in Get-ChildItem -LiteralPath $workshop -Filter '*.bbcode' -File) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
    foreach ($tag in $tags) {
        $open = [regex]::Matches($text, "(?i)\[$tag(?:=[^\]]+)?\]").Count
        $close = [regex]::Matches($text, "(?i)\[/$tag\]").Count
        if ($open -ne $close) {
            throw "$($file.Name) has unbalanced [$tag] tags: $open open, $close close."
        }
    }
    if ($text -match '(?i)\bsk-(?:proj-|ant-)?[a-z0-9_-]{20,}' -or
            $text -match '(?i)C:\\Users\\[^\\\s`]+' -or
            $text -match '"apiKey"\s*:\s*"(?!")[^"]+"') {
        throw "$($file.Name) contains possible private data."
    }
}

Write-Host "Workshop checks passed: version $manifestVersion, $((Get-ChildItem $workshop -Filter '*.bbcode').Count) descriptions."
