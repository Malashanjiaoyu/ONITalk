param(
    [string[]]$RequiredLanguages = @('zh', 'es')
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$translationRoot = Join-Path $repositoryRoot 'src\ONITalk.Mod\translations'
$templatePath = Join-Path $translationRoot 'template.pot'
$infrastructureRoot = Join-Path $repositoryRoot 'src\ONITalk.Mod\Infrastructure'
$stringsPath = Join-Path $repositoryRoot 'src\ONITalk.Mod\Localization\STRINGS.cs'

function Read-PoEntries {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Localization file not found: $Path"
    }

    $entries = @{}
    $context = $null
    $messageId = $null
    foreach ($line in Get-Content -Encoding UTF8 -LiteralPath $Path) {
        if ($line -match '^msgctxt "(?<value>.*)"$') {
            $context = $Matches.value
            $messageId = $null
        } elseif ($null -ne $context -and $line -match '^msgid "(?<value>.*)"$') {
            $messageId = $Matches.value
        } elseif ($null -ne $context -and $line -match '^msgstr "(?<value>.*)"$') {
            if ($entries.ContainsKey($context)) {
                throw "Duplicate localization key '$context' in $Path"
            }
            $entries[$context] = [PSCustomObject]@{
                MessageId = $messageId
                Translation = $Matches.value
            }
            $context = $null
            $messageId = $null
        }
    }
    return $entries
}

$template = Read-PoEntries $templatePath
if ($template.Count -eq 0) {
    throw 'Localization template contains no keyed entries.'
}

$stringsText = Get-Content -Encoding UTF8 -Raw -LiteralPath $stringsPath
$locStringFieldCount = [regex]::Matches($stringsText,
    'public static LocString\s+[A-Z0-9_]+\s*=').Count
if ($locStringFieldCount -ne $template.Count) {
    throw ("STRINGS.cs declares {0} LocString fields but template.pot contains {1} keys." -f
        $locStringFieldCount, $template.Count)
}

$keySourceText = (Get-ChildItem -LiteralPath $infrastructureRoot -Filter '*.cs' -File |
    ForEach-Object { Get-Content -Encoding UTF8 -Raw -LiteralPath $_.FullName }) -join "`n"
$usedKeys = [regex]::Matches($keySourceText, '"(?<key>STRINGS\.ONITALK\.[A-Z0-9_.]+)"') |
    ForEach-Object { $_.Groups['key'].Value } |
    Sort-Object -Unique
foreach ($key in $usedKeys) {
    if (-not $template.ContainsKey($key)) {
        throw "Option key '$key' is missing from template.pot"
    }
}

foreach ($language in $RequiredLanguages) {
    $path = Join-Path $translationRoot ($language + '.po')
    $translation = Read-PoEntries $path
    foreach ($key in $template.Keys) {
        if (-not $translation.ContainsKey($key)) {
            throw "Translation '$language' is missing key '$key'"
        }
        if ([string]::IsNullOrWhiteSpace($translation[$key].Translation)) {
            throw "Translation '$language' has an empty value for '$key'"
        }
        if ($translation[$key].MessageId -ne $template[$key].MessageId) {
            throw "Translation '$language' has a stale msgid for '$key'"
        }
        $sourceVariables = @([regex]::Matches($template[$key].MessageId,
            '\{[A-Za-z0-9_]+\}') | ForEach-Object { $_.Value } | Sort-Object)
        $translatedVariables = @([regex]::Matches($translation[$key].Translation,
            '\{[A-Za-z0-9_]+\}') | ForEach-Object { $_.Value } | Sort-Object)
        if (($sourceVariables -join ',') -ne ($translatedVariables -join ',')) {
            throw "Translation '$language' changes format variables for '$key'"
        }
    }
    foreach ($key in $translation.Keys) {
        if (-not $template.ContainsKey($key)) {
            throw "Translation '$language' contains unknown key '$key'"
        }
    }
}

$runtimeFiles = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src\ONITalk.Mod') `
    -Recurse -Filter '*.cs' -File | Where-Object { $_.Name -ne 'STRINGS.cs' }
$unsafeRuntimePatterns = @(
    '(?m)\b(?:Text|ToolTip|text)\s*=\s*STRINGS\.ONITALK',
    '(?m)\breturn\s+STRINGS\.ONITALK',
    'string\.Format\(\s*STRINGS\.ONITALK'
)
foreach ($file in $runtimeFiles) {
    $source = Get-Content -Encoding UTF8 -Raw -LiteralPath $file.FullName
    foreach ($pattern in $unsafeRuntimePatterns) {
        if ($source -match $pattern) {
            $relative = $file.FullName.Substring($repositoryRoot.Length + 1)
            throw "Runtime localization bypasses ONITalkLocalization in $relative"
        }
    }
}

Write-Host ("Localization checks passed: {0} keys, languages: {1}" -f
    $template.Count, ($RequiredLanguages -join ', '))
