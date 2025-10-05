<#
.SYNOPSIS
  Builds a protected version of UWUVCI AIO WPF with version bump,
  AES key rotation, and LocalInstallGuard enforcement.

.DESCRIPTION
  - Rotates AES key parts in LocalInstallGuard.cs via Rotate-InstallKey.ps1
  - Bumps AssemblyVersion / <Version> automatically
  - Enables EnforceProtection
  - Builds in Release mode
  - Optionally zips output
  - Restores originals afterward

.NOTES
  Requires PowerShell 7+ and dotnet SDK installed.
#>

param(
    [string]$Project = "UWUVCI AIO WPF",
    [string]$Configuration = "Release",
    [switch]$ZipOutput
)

Write-Host "=== Building protected Release of $Project ===" -ForegroundColor Cyan

# ---- STEP 0: Rotate AES Key Parts ----
$rotateScript = Join-Path (Join-Path (Get-Location) "Scripts") "Rotate-InstallKey.ps1"
if (Test-Path $rotateScript) {
    Write-Host "🔁 Running Rotate-InstallKey.ps1..."
    & $rotateScript
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Rotate-InstallKey.ps1 failed."
        exit 1
    }
} else {
    Write-Warning "⚠️ Rotate-InstallKey.ps1 not found — skipping rotation."
}

# ---- STEP 1: Locate required files ----
$guardFile = Get-ChildItem -Path . -Recurse -Filter "LocalInstallGuard.cs" | Select-Object -First 1
if (-not $guardFile) { Write-Error "❌ Could not find LocalInstallGuard.cs!"; exit 1 }

$solution = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
if (-not $solution) { Write-Error "❌ No .sln file found!"; exit 1 }

$assemblyInfo = Get-ChildItem -Recurse -Filter "AssemblyInfo.cs" | Select-Object -First 1
$projFile = Get-ChildItem -Recurse -Filter "*.csproj" | Select-Object -First 1

# ---- STEP 2: Backup originals ----
$backupGuard = "$($guardFile.FullName).bak"
Copy-Item $guardFile.FullName $backupGuard -Force

$backupAsm = $null
if ($assemblyInfo) {
    $backupAsm = "$($assemblyInfo.FullName).bak"
    Copy-Item $assemblyInfo.FullName $backupAsm -Force
}
elseif ($projFile) {
    $backupAsm = "$($projFile.FullName).bak"
    Copy-Item $projFile.FullName $backupAsm -Force
}

# ---- STEP 3: Helper functions ----
function Get-AppVersion {
    if ($assemblyInfo) {
        $match = Select-String -Path $assemblyInfo.FullName -Pattern 'AssemblyVersion\("([^"]+)"\)' | Select-Object -First 1
        if ($match) { return $match.Matches[0].Groups[1].Value }
    }
    elseif ($projFile) {
        $xml = [xml](Get-Content $projFile.FullName -Raw)
        return $xml.Project.PropertyGroup.Version
    }
    return "0.0.0.0"
}

function Bump-Version($version) {
    $parts = $version.Split('.')
    if ($parts.Count -lt 4) { while ($parts.Count -lt 4) { $parts += '0' } }
    $parts[3] = [int]$parts[3] + 1
    return ($parts -join '.')
}

function Set-NewVersion($newVersion) {
    if ($assemblyInfo) {
        (Get-Content $assemblyInfo.FullName -Raw) -replace 'AssemblyVersion\("([^"]+)"\)', "AssemblyVersion(`"$newVersion`")" |
            Set-Content $assemblyInfo.FullName -Encoding UTF8
        (Get-Content $assemblyInfo.FullName -Raw) -replace 'AssemblyFileVersion\("([^"]+)"\)', "AssemblyFileVersion(`"$newVersion`")" |
            Set-Content $assemblyInfo.FullName -Encoding UTF8
    }
    elseif ($projFile) {
        $xml = [xml](Get-Content $projFile.FullName -Raw)
        $xml.Project.PropertyGroup.Version = $newVersion
        $xml.Save($projFile.FullName)
    }
}

# ---- STEP 4: Enable Protection ----
$content = Get-Content $guardFile.FullName -Raw
$content = $content -replace "const bool EnforceProtection = false;", "const bool EnforceProtection = true;"
Set-Content -Path $guardFile.FullName -Value $content -Encoding UTF8
Write-Host "🧩 LocalInstallGuard protection enabled."

# ---- STEP 5: Bump version ----
$oldVersion = Get-AppVersion
$newVersion = Bump-Version $oldVersion
Set-NewVersion $newVersion
Write-Host "📦 Version bumped: $oldVersion → $newVersion"

# ---- STEP 6: Build ----
Write-Host "🏗️ Building $($solution.Name) ($Configuration)..."
dotnet build $solution.FullName -c $Configuration /p:Platform="Any CPU"

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Build failed."
    Move-Item $backupGuard $guardFile.FullName -Force
    if ($backupAsm) { Move-Item $backupAsm ($backupAsm -replace '.bak$', '') -Force }
    exit 1
}
Write-Host "✅ Build completed."

# ---- STEP 7: Optional ZIP ----
if ($ZipOutput) {
    $outputDir = Join-Path "bin" $Configuration
    $timestamp = Get-Date -Format "yyyyMMdd_HHmm"
    $zipName = "UWUVCI_protectedBuild_v$newVersion_$timestamp.zip"
    Write-Host "📦 Creating archive $zipName..."
    Compress-Archive -Path "$outputDir/*" -DestinationPath $zipName -Force
}

# ---- STEP 7.5: Record release metadata ----
$manifestPath = Join-Path "Scripts" "release-manifest.json"
if (-not (Test-Path $manifestPath)) {
    "[]" | Set-Content -Path $manifestPath -Encoding UTF8
}

try {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if (-not $manifest) { $manifest = @() }

    # Compute SHA256 hash of AES key parts in LocalInstallGuard
    $guardContent = Get-Content $guardFile.FullName -Raw
    $match = [regex]::Match($guardContent, 'string p1 = "([^"]+)";.*?p2 = "([^"]+)";.*?p3 = "([^"]+)";.*?p4 = "([^"]+)"', 'Singleline')
    if ($match.Success) {
        $combined = ($match.Groups[1].Value + $match.Groups[2].Value + $match.Groups[3].Value + $match.Groups[4].Value)
        $sha256 = [System.BitConverter]::ToString(
            ([System.Security.Cryptography.SHA256]::Create()).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($combined))
        ).Replace("-", "").ToLower()
    } else {
        $sha256 = "unknown"
    }

    $entry = [ordered]@{
        version    = $newVersion
        timestamp  = (Get-Date).ToUniversalTime().ToString("o")
        aesKeyHash = $sha256
        zipped     = [bool]$ZipOutput
    }

    $manifest += [pscustomobject]$entry
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding UTF8
    Write-Host "🧾 Release manifest updated → $manifestPath"
}
catch {
    Write-Warning "⚠️ Failed to update release manifest: $_"
}


# ---- STEP 8: Restore original files ----
Move-Item $backupGuard $guardFile.FullName -Force
if ($backupAsm) {
    Move-Item $backupAsm ($backupAsm -replace '.bak$', '') -Force
}

Write-Host "♻️ LocalInstallGuard + version restored."
Write-Host "🎉 Protected build complete: version $newVersion" -ForegroundColor Green
