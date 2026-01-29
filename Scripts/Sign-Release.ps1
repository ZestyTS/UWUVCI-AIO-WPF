<#
.SYNOPSIS
  Signs a built EXE with the release private key and writes uwuvci.sig.

.DESCRIPTION
  Uses RSA-SHA256. The signature is base64 and saved next to the EXE.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,
    [string]$PrivateKeyPath = (Join-Path $PSScriptRoot "keys\\release-private.xml"),
    [string]$SignatureFileName = "uwuvci.sig"
)

if (-not (Test-Path $ExePath)) {
    Write-Error "❌ EXE not found: $ExePath"
    exit 1
}

if (-not (Test-Path $PrivateKeyPath)) {
    Write-Error "❌ Private key not found: $PrivateKeyPath"
    exit 1
}

$privateXml = Get-Content $PrivateKeyPath -Raw
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.FromXmlString($privateXml)

$sha = [System.Security.Cryptography.SHA256]::Create()
$stream = [System.IO.File]::OpenRead($ExePath)
try {
    $hash = $sha.ComputeHash($stream)
}
finally {
    $stream.Dispose()
}

$signature = $rsa.SignHash($hash, "SHA256")
$sigBase64 = [System.Convert]::ToBase64String($signature)

$sigPath = Join-Path (Split-Path $ExePath -Parent) $SignatureFileName
Set-Content -Path $sigPath -Value $sigBase64 -Encoding UTF8

Write-Host "✅ Signature written: $sigPath"
