$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$excluded = '[\\/](?:\.git|\.agents|\.codex|\.obsidian|\.dotnet-home|artifacts|bin|obj|\.vs|\.idea)[\\/]'
$textExtensions = @('.cs', '.csproj', '.json', '.md', '.po', '.pot', '.ps1',
    '.txt', '.yaml', '.yml', '.gitignore', '.gitattributes')
$patterns = [ordered]@{
    'OpenAI-compatible API key' = '(?i)\bsk-(?:proj-|ant-)?[a-z0-9_-]{20,}'
    'Google API key' = '\bAIza[0-9A-Za-z_-]{25,}'
    'GitHub token' = '\bgh[pousr]_[0-9A-Za-z]{20,}'
    'Private key block' = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
    'Non-empty JSON API key' = '"apiKey"\s*:\s*"(?!")[^"]+"'
    'Personal Windows user path' = '(?i)C:\\Users\\(?!Public(?:\\|$))[^\\\s`]+'
}

$findings = [System.Collections.Generic.List[string]]::new()
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
    if ($file.FullName -match $excluded)
        { continue }
    $extension = $file.Extension.ToLowerInvariant()
    if ($file.Name -notin @('.gitignore', '.gitattributes') -and
            $extension -notin $textExtensions)
        { continue }
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
    foreach ($entry in $patterns.GetEnumerator()) {
        if ($text -match $entry.Value) {
            $relative = $file.FullName.Substring($root.Length + 1)
            $findings.Add("${relative}: $($entry.Key)")
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | ForEach-Object { Write-Error $_ }
    throw "Secret/privacy scan failed with $($findings.Count) finding(s)."
}

Write-Host 'Secret/privacy scan passed.'
