$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$frontendDirectory = Join-Path $projectRoot 'frontend'
$apiProject = Join-Path $projectRoot 'backend\src\BonusTest.Api\BonusTest.Api.csproj'
$runDirectory = Join-Path $projectRoot '.run'
$logDirectory = Join-Path $runDirectory 'logs'
$apiUrl = 'http://localhost:5106'
$frontendUrl = 'http://localhost:4200'

function Assert-NodeVersion {
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    if (-not $nodeCommand) {
        throw 'No se encontró Node.js. Instala Node.js 24.15+ o 22.22.3+.'
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
}

function Assert-Dotnet10 {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw 'No se encontró el SDK de .NET 10. Ejecuta INSTALAR_DEPENDENCIAS.bat después de instalarlo.'
    }

    $sdkList = @(& $dotnetCommand.Source --list-sdks 2>$null)
    if (-not ($sdkList | Where-Object { $_ -match '^\s*10\.\d+\.\d+' })) {
        throw 'El proyecto requiere el SDK de .NET 10. Instala el SDK antes de iniciar la aplicación.'
    }
}

function Save-ProcessRecord($process, [string]$name) {
    $record = [pscustomobject]@{
        processId = [int]$process.Id
        processName = $name
        startTimeUtc = $process.StartTime.ToUniversalTime().ToString('o')
    }
    $record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runDirectory ($name + '.json')) -Encoding UTF8
}

function Wait-ForUrl([string]$url, [string]$serviceName, [int]$timeoutSeconds) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $lastError = ''
    while ($watch.Elapsed.TotalSeconds -lt $timeoutSeconds) {
        try {
            $response = Invoke-WebRequest -Uri $url -TimeoutSec 3 -UseBasicParsing
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                Write-Host "$serviceName está disponible: $url"
                return
            }
            $lastError = "HTTP $($response.StatusCode)"
        }
        catch {
            $lastError = $_.Exception.Message
        }
        Start-Sleep -Seconds 2
    }

    throw "No respondió $serviceName en $timeoutSeconds segundos. Último estado: $lastError"
}

function Stop-StartedProcess($process, [string]$name) {
    if ($process -and -not $process.HasExited) {
        & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
    }
    Remove-Item -LiteralPath (Join-Path $runDirectory ($name + '.json')) -Force -ErrorAction SilentlyContinue
}

$apiProcess = $null
$frontendProcess = $null
try {
    Assert-NodeVersion
    Assert-Dotnet10

    $npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $npmCommand) {
        throw 'No se encontró npm.cmd. Instala Node.js incluyendo npm.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $frontendDirectory 'node_modules\@angular\build\package.json'))) {
        throw 'Faltan las dependencias de Angular. Ejecuta INSTALAR_DEPENDENCIAS.bat.'
    }

    $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -in @(5106, 4200) })
    if ($listeners.Count -gt 0) {
        $ports = ($listeners.LocalPort | Sort-Object -Unique) -join ', '
        throw "Los puertos $ports ya están en uso. Ejecuta DETENER_APLICACION.bat o cierra el proceso que los ocupa."
    }

    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $apiStdout = Join-Path $logDirectory 'api.stdout.log'
    $apiStderr = Join-Path $logDirectory 'api.stderr.log'
    $frontendStdout = Join-Path $logDirectory 'frontend.stdout.log'
    $frontendStderr = Join-Path $logDirectory 'frontend.stderr.log'

    $apiArguments = 'run --project "{0}" --no-launch-profile -- --urls {1}' -f $apiProject, $apiUrl
    $apiProcess = Start-Process -FilePath $dotnetCommand.Source -ArgumentList $apiArguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $apiStdout -RedirectStandardError $apiStderr
    Save-ProcessRecord $apiProcess 'api'
    Write-Host 'Iniciando la API...'
    Wait-ForUrl ($apiUrl + '/api/v1/Catalog/departments') 'API y SQL Server' 90

    $frontendArguments = '/d /s /c "npm.cmd run start -- --host localhost --port 4200"'
    $frontendProcess = Start-Process -FilePath $env:ComSpec -ArgumentList $frontendArguments -WorkingDirectory $frontendDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput $frontendStdout -RedirectStandardError $frontendStderr
    Save-ProcessRecord $frontendProcess 'frontend'
    Write-Host 'Iniciando Angular...'
    Wait-ForUrl $frontendUrl 'Interfaz web' 90

    Start-Process $frontendUrl
    Write-Host 'Aplicación iniciada. Mantén abierta esta ventana para consultar el estado.'
    Write-Host "Registros de ejecución: $logDirectory"
}
catch {
    Stop-StartedProcess $frontendProcess 'frontend'
    Stop-StartedProcess $apiProcess 'api'
    Write-Host "No se pudo iniciar la aplicación: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Revisa .run\logs para consultar la salida de la API y Angular.'
    exit 1
}
