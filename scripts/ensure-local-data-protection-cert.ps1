$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$certDirectory = Join-Path $repoRoot 'ops\docker\data-protection'
$certPath = Join-Path $certDirectory 'aspnet-dp.pfx'
$password = if ($env:DATA_PROTECTION_CERT_PASSWORD) { $env:DATA_PROTECTION_CERT_PASSWORD } else { 'local-dev-data-protection' }

if (Test-Path $certPath) {
    Write-Host "Data Protection certificate already present at $certPath" -ForegroundColor DarkGray
    return
}

New-Item -ItemType Directory -Path $certDirectory -Force | Out-Null

$cert = New-SelfSignedCertificate `
    -DnsName 'local.data-protection.ambev' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -FriendlyName 'Ambev Developer Evaluation Local Data Protection' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(5)

$securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath $certPath -Password $securePassword | Out-Null

Write-Host "Generated local Data Protection certificate at $certPath" -ForegroundColor Green