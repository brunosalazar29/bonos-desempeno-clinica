$ErrorActionPreference = 'Continue'
$projectRoot = Split-Path -Parent $PSScriptRoot
$runDirectory = Join-Path $projectRoot '.run'

function Stop-RecordedProcess([string]$recordPath, [string[]]$expectedNames) {
    if (-not (Test-Path -LiteralPath $recordPath)) {
        return
    }

    try {
        $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
        $process = Get-Process -Id ([int]$record.processId) -ErrorAction SilentlyContinue
        if ($process -and $expectedNames -contains $process.ProcessName) {
            $recordedStart = [DateTime]::Parse($record.startTimeUtc).ToUniversalTime()
            $actualStart = $process.StartTime.ToUniversalTime()
            if ([Math]::Abs(($actualStart - $recordedStart).TotalSeconds) -lt 5) {
                & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
                Write-Host "Servicio detenido (PID $($process.Id))."
            }
        }
    }
    catch {
        Write-Host "No se pudo detener un proceso registrado: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    finally {
        Remove-Item -LiteralPath $recordPath -Force -ErrorAction SilentlyContinue
    }
}

Stop-RecordedProcess (Join-Path $runDirectory 'frontend.json') @('cmd')
Stop-RecordedProcess (Join-Path $runDirectory 'api.json') @('dotnet')
Write-Host 'Finalizó la detención de los servicios de esta aplicación.'
