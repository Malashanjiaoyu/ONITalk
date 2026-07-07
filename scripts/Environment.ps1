Set-StrictMode -Version 2.0

function Get-SteamRoots {
    $roots = New-Object System.Collections.Generic.List[string]
    $registryPaths = @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )

    foreach ($registryPath in $registryPaths) {
        if (-not (Test-Path $registryPath)) {
            continue
        }

        $steam = Get-ItemProperty $registryPath -ErrorAction SilentlyContinue
        foreach ($propertyName in @('SteamPath', 'InstallPath')) {
            $property = $steam.PSObject.Properties[$propertyName]
            if ($null -eq $property) {
                continue
            }
            $value = $property.Value
            if (-not [string]::IsNullOrWhiteSpace($value) -and
                    (Test-Path -LiteralPath $value)) {
                $roots.Add([System.IO.Path]::GetFullPath($value))
            }
        }
    }

    foreach ($candidate in @(
        "$env:ProgramFiles\Steam",
        "${env:ProgramFiles(x86)}\Steam",
        'C:\Steam', 'D:\Steam', 'E:\Steam', 'F:\Steam'
    )) {
        if (Test-Path -LiteralPath $candidate) {
            $roots.Add([System.IO.Path]::GetFullPath($candidate))
        }
    }

    return @($roots | Select-Object -Unique)
}

function Get-SteamLibraries {
    $libraries = New-Object System.Collections.Generic.List[string]
    foreach ($steamRoot in Get-SteamRoots) {
        $libraries.Add($steamRoot)
        $vdfPath = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path -LiteralPath $vdfPath)) {
            continue
        }

        $content = Get-Content -LiteralPath $vdfPath -Raw -ErrorAction SilentlyContinue
        if ([string]::IsNullOrWhiteSpace($content)) {
            continue
        }

        foreach ($match in [regex]::Matches($content, '"path"\s+"(?<path>[^"]+)"')) {
            $path = $match.Groups['path'].Value.Replace('\\', '\')
            if (Test-Path -LiteralPath $path) {
                $libraries.Add([System.IO.Path]::GetFullPath($path))
            }
        }
    }

    return @($libraries | Select-Object -Unique)
}

function Test-ONIGameDirectory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $assembly = Join-Path $Path 'OxygenNotIncluded_Data\Managed\Assembly-CSharp.dll'
    return Test-Path -LiteralPath $assembly
}

function Resolve-ONIGameDirectory {
    param([string]$ExplicitPath)

    foreach ($candidate in @($ExplicitPath, $env:ONI_GAME_DIR)) {
        if (Test-ONIGameDirectory $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    foreach ($library in Get-SteamLibraries) {
        $steamApps = Join-Path $library 'steamapps'
        $manifest = Join-Path $steamApps 'appmanifest_457140.acf'
        $installDir = 'OxygenNotIncluded'
        if (Test-Path -LiteralPath $manifest) {
            $content = Get-Content -LiteralPath $manifest -Raw -ErrorAction SilentlyContinue
            $match = [regex]::Match($content, '"installdir"\s+"(?<name>[^"]+)"')
            if ($match.Success) {
                $installDir = $match.Groups['name'].Value
            }
        }

        $candidate = Join-Path (Join-Path $steamApps 'common') $installDir
        if (Test-ONIGameDirectory $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    return $null
}

function Resolve-ONIUserDataDirectory {
    $userRoot = $env:USERPROFILE
    if ([string]::IsNullOrWhiteSpace($userRoot)) {
        $userRoot = [Environment]::GetFolderPath('UserProfile')
    }
    $documents = [Environment]::GetFolderPath('MyDocuments')
    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in @(
        (Join-Path $documents 'Klei\OxygenNotIncluded'),
        (Join-Path $userRoot 'OneDrive\Documents\Klei\OxygenNotIncluded')
    )) {
        $candidates.Add($candidate)
    }

    $oneDrive = Join-Path $userRoot 'OneDrive'
    if (Test-Path -LiteralPath $oneDrive) {
        foreach ($folder in Get-ChildItem -LiteralPath $oneDrive -Directory -ErrorAction SilentlyContinue) {
            $candidates.Add((Join-Path $folder.FullName 'Klei\OxygenNotIncluded'))
        }
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    return $null
}

function Resolve-DotnetExecutable {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path -LiteralPath $ExplicitPath) {
            return [System.IO.Path]::GetFullPath($ExplicitPath)
        }
        return $null
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($env:DOTNET_ROOT)) {
        $candidates.Add((Join-Path $env:DOTNET_ROOT 'dotnet.exe'))
    }
    $candidates.Add('D:\Software\dotnet8\dotnet.exe')

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }
    return $null
}

function Resolve-GitExecutable {
    $command = Get-Command git -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    foreach ($candidate in @(
        'D:\Software\Git\cmd\git.exe',
        'D:\Software\Git\bin\git.exe'
    )) {
        if (Test-Path -LiteralPath $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }
    return $null
}

function Get-DotnetSdkVersions {
    param([string]$DotnetPath)

    if ([string]::IsNullOrWhiteSpace($DotnetPath)) {
        return @()
    }

    return @(& $DotnetPath --list-sdks 2>$null)
}

function Test-Dotnet8Sdk {
    param([string]$DotnetPath)

    foreach ($line in Get-DotnetSdkVersions $DotnetPath) {
        if ($line -match '^8\.') {
            return $true
        }
    }
    return $false
}
