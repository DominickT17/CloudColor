param([string]$Sha, [switch]$Reiniciar)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'utilidades.ps1')
if (($Sha -and $Reiniciar) -or (-not $Sha -and -not $Reiniciar)) { throw 'Indique -Sha o -Reiniciar.' }
Assert-CloudColorContext
[void](Assert-WebAvailable)
$monitor = Start-CloudColorMonitor
$clock = [Diagnostics.Stopwatch]::StartNew()
$operationFailed = $false
try {
    if ($Sha) {
        & (Join-Path $PSScriptRoot 'desplegar-sha.ps1') -Sha $Sha
    } else {
        kubectl --context cloudcolor -n cloudcolor rollout restart deployment/cloudcolor-web
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar el rollout.' }
        kubectl --context cloudcolor -n cloudcolor rollout status deployment/cloudcolor-web --timeout=180s
        if ($LASTEXITCODE -ne 0) { throw 'El rollout no termino correctamente.' }
        [void](Assert-WebAvailable)
    }
    # Observar tambien la retirada de los endpoints inmediatamente posterior al rollout.
    Start-Sleep -Seconds 6
} catch { $operationFailed = $true; Write-Output 'El rollout no termino correctamente.' }
finally {
    $clock.Stop()
    $results = Stop-CloudColorMonitor $monitor
    Write-Output "Tiempo: $([math]::Round($clock.Elapsed.TotalSeconds,1)) s; solicitudes: $($results.Total); exitosas: $($results.Success); fallos: $($results.Failures)."
}
if ($operationFailed -or $results.Total -lt 1 -or $results.Failures -ne 0) { exit 1 }
Write-Output 'Rollout terminado con dos Pods disponibles y sin fallos observados.'
