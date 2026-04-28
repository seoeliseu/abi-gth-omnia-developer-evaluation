$ErrorActionPreference = 'Stop'

$certScript = Join-Path $PSScriptRoot 'ensure-local-data-protection-cert.ps1'

Write-Host 'Ensuring local Data Protection certificate...' -ForegroundColor Cyan
& $certScript

$services = @(
    'products-api',
    'auth-api',
    'users-api',
    'carts-api',
    'sales-api'
)

Write-Host 'Starting infrastructure containers...' -ForegroundColor Cyan
docker compose up -d postgres mongodb rabbitmq redis seq

foreach ($service in $services) {
    Write-Host "Building $service..." -ForegroundColor Cyan
    docker compose build $service
}

Write-Host 'Starting full application stack...' -ForegroundColor Cyan
docker compose up -d

Write-Host 'Current compose status:' -ForegroundColor Cyan
docker compose ps