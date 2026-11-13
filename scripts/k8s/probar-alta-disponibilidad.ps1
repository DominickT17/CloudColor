param([switch]$EliminarUnPod)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'utilidades.ps1')
Assert-CloudColorContext
$initial = Assert-WebAvailable
kubectl --context cloudcolor -n cloudcolor get pods -l app=cloudcolor-web -o wide
if (-not $EliminarUnPod) { Write-Output 'Use -EliminarUnPod para ejecutar la prueba controlada.'; exit 0 }
$target = $initial.Pods[0].metadata.name
$originalIds = @($initial.Pods | ForEach-Object { $_.metadata.uid })
$monitor = Start-CloudColorMonitor
$clock = [Diagnostics.Stopwatch]::StartNew()
$minimumEndpoints = [int]::MaxValue
$success = $false
try {
    # Solo se borra el Pod seleccionado de la web, nunca SQL Server.
    kubectl --context cloudcolor -n cloudcolor delete pod $target --wait=false
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar el Pod web.' }
    while ($clock.Elapsed.TotalSeconds -lt 180) {
        $state = Get-WebState
        $minimumEndpoints = [Math]::Min($minimumEndpoints, $state.Endpoints.Count)
        $newPods = @($state.Pods | Where-Object { $_.metadata.uid -notin $originalIds })
        if ($newPods.Count -gt 0 -and $state.Pods.Count -ge 2 -and $state.Endpoints.Count -ge 2 -and $state.Deployment.status.availableReplicas -ge 2) { $success=$true; break }
        Start-Sleep -Seconds 1
    }
} finally {
    $clock.Stop()
    $results = Stop-CloudColorMonitor $monitor
    Write-Output "Tiempo: $([math]::Round($clock.Elapsed.TotalSeconds,1)) s; solicitudes: $($results.Total); exitosas: $($results.Success); fallos: $($results.Failures); minimo endpoints: $minimumEndpoints."
}
if (-not $success -or $minimumEndpoints -lt 1 -or $results.Total -lt 1 -or $results.Failures -ne 0) { throw 'La prueba no cumplio todas las condiciones; revise el estado del clúster.' }
kubectl --context cloudcolor -n cloudcolor get pods -l app=cloudcolor-web -o wide
Write-Output 'Un Pod reemplazado; dos Pods disponibles y sin fallos observados.'
