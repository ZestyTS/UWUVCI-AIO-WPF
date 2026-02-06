<#
.SYNOPSIS
  Injects the release public key into ReleaseSignatureVerifier.cs.

.DESCRIPTION
  Reads Scripts/keys/release-public.xml and replaces the region between
  // BEGIN_PUBLIC_KEY and // END_PUBLIC_KEY in ReleaseSignatureVerifier.cs.
#>

[CmdletBinding()]
param(
    [string]$PublicKeyPath = (Join-Path $PSScriptRoot "keys\\release-public.xml")
)

$targetFile = Get-ChildItem -Path (Join-Path $PSScriptRoot "..") -Recurse -Filter "ReleaseSignatureVerifier.cs" | Select-Object -First 1
if (-not $targetFile) {
    Write-Error "❌ Could not find ReleaseSignatureVerifier.cs"
    exit 1
}

if (-not (Test-Path $PublicKeyPath)) {
    Write-Error "❌ Public key not found: $PublicKeyPath"
    exit 1
}

$publicXml = Get-Content $PublicKeyPath -Raw
$escaped = $publicXml -replace '"', '""'
$replacement = @"
// BEGIN_PUBLIC_KEY
        private const string PublicKeyXml = @"$escaped";
// END_PUBLIC_KEY
"@

$content = Get-Content $targetFile.FullName -Raw
$newContent = [regex]::Replace($content, '(?s)// BEGIN_PUBLIC_KEY.*?// END_PUBLIC_KEY', $replacement)

if ($content -eq $newContent) {
    Write-Error "❌ Failed to inject public key. Marker region not found."
    exit 1
}

Set-Content -Path $targetFile.FullName -Value $newContent -Encoding UTF8
Write-Host "✅ Public key injected into ReleaseSignatureVerifier.cs"
