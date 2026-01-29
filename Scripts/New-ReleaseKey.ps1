<#
.SYNOPSIS
  Generates a new RSA keypair for release signing.

.DESCRIPTION
  Creates Scripts/keys/release-private.xml and release-public.xml.
  Keep the private key secret and out of git.
#>

[CmdletBinding()]
param(
    [int]$KeySize = 3072
)

$keysDir = Join-Path $PSScriptRoot "keys"
New-Item -ItemType Directory -Force -Path $keysDir | Out-Null

$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider $KeySize
$privateXml = $rsa.ToXmlString($true)
$publicXml = $rsa.ToXmlString($false)

$privatePath = Join-Path $keysDir "release-private.xml"
$publicPath = Join-Path $keysDir "release-public.xml"

Set-Content -Path $privatePath -Value $privateXml -Encoding UTF8
Set-Content -Path $publicPath -Value $publicXml -Encoding UTF8

Write-Host "✅ Release keypair generated."
Write-Host "   Private: $privatePath"
Write-Host "   Public : $publicPath"
Write-Warning "Keep release-private.xml secret and out of git."
