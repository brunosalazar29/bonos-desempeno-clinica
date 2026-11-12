$ErrorActionPreference = 'Stop'

function Get-NodeVersion {
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    if (-not $nodeCommand) {
        throw 'No se encontró Node.js. Instala Node.js 24.15+ o 22.22.3+ y vuelve a intentarlo.'
    }

    $versionText = (& $nodeCommand.Source --version 2>$null).Trim()
    $match = [regex]::Match($versionText, '^v?(\d+)\.(\d+)\.(\d+)')
    if (-not $match.Success) {
        throw "No se pudo identificar la versión de Node.js: $versionText"
    }

    $major = [int]$match.Groups[1].Value
    $minor = [int]$match.Groups[2].Value
    $patch = [int]$match.Groups[3].Value
    $supported = (($major -eq 22 -and ($minor -gt 22 -or ($minor -eq 22 -and $patch -ge 3))) -or ($major -eq 24 -and $minor -ge 15) -or ($major -ge 26))
    if (-not $supported) {
        throw "Node.js $versionText no cumple el mínimo de Angular 22. Instala Node.js 24.15+ o 22.22.3+."
    }

    return $versionText
}

function Assert-Dotnet10 {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw 'No se encontró el SDK de .NET 10. Instala .NET 10 SDK y vuelve a intentarlo.'
    }

    $sdkList = @(& $dotnetCommand.Source --list-sdks 2>$null)
    if (-not ($sdkList | Where-Object { $_ -match '^\s*10\.\d+\.\d+' })) {
        throw 'El proyecto requiere el SDK de .NET 10. Instala el SDK y vuelve a intentarlo.'
    }
}

try {
    $projectRoot = Split-Path -Parent $PSScriptRoot
    $nodeVersion = Get-NodeVersion
    Assert-Dotnet10

    $npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
    if (-not $npmCommand) {
        throw 'No se encontró npm.cmd. Reinstala Node.js incluyendo npm y abre una consola nueva.'
    }

    Write-Host "Node.js $nodeVersion detectado."
    Write-Host 'Instalando las dependencias fijadas en package-lock.json...'
    Push-Location (Join-Path $projectRoot 'frontend')
    try {
        & $npmCommand.Source ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci terminó con el código $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    Write-Host 'Restaurando los paquetes de .NET...'
    & dotnet restore (Join-Path $projectRoot 'backend\BonusTest.slnx')
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore terminó con el código $LASTEXITCODE."
    }

    Write-Host 'Dependencias instaladas.'
}
catch {
    Write-Host "No se pudieron instalar las dependencias: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
