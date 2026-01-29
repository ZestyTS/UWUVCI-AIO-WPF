param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,

    [Parameter(Mandatory = $true)]
    [string]$PrivateKeyPath,

    [string]$SigPath
)

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "EXE not found: $ExePath"
}

if (-not (Test-Path -LiteralPath $PrivateKeyPath)) {
    throw "Private key not found: $PrivateKeyPath"
}

if ([string]::IsNullOrWhiteSpace($SigPath)) {
    $SigPath = Join-Path -Path (Split-Path -Parent $ExePath) -ChildPath "uwuvci.sig"
}

$privateKeyXml = Get-Content -LiteralPath $PrivateKeyPath -Raw

$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.FromXmlString($privateKeyXml)

$sha = [System.Security.Cryptography.SHA256]::Create()
$stream = [System.IO.File]::OpenRead($ExePath)
try {
    $hash = $sha.ComputeHash($stream)
}
finally {
    $stream.Dispose()
}

$signature = $rsa.SignHash($hash, [System.Security.Cryptography.CryptoConfig]::MapNameToOID("SHA256"))
$signatureB64 = [Convert]::ToBase64String($signature)

Set-Content -LiteralPath $SigPath -Value $signatureB64
Write-Host "Signature written to $SigPath"
