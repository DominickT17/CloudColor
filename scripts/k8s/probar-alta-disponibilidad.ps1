param([switch]$EliminarUnPod)
$ErrorActionPreference = 'Stop'
$ns = 'cloudcolor'
kubectl -n $ns get pods -l app=cloudcolor-web -o wide
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los Pods web.' }
if (-not $EliminarUnPod) {
    Write-Output 'Para eliminar un unico Pod web, vuelva a ejecutar con -EliminarUnPod.'
    exit 0
}
$json = kubectl -n $ns get pods -l app=cloudcolor-web -o json
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los Pods web.' }
$pods = ($json | ConvertFrom-Json).items | Where-Object {
    $_.metadata.deletionTimestamp -eq $null -and $_.status.phase -eq 'Running' -and
    @($_.status.conditions | Where-Object { $_.type -eq 'Ready' -and $_.status -eq 'True' }).Count -gt 0
}
if (@($pods).Count -lt 2) { throw 'Se requieren al menos dos Pods web Ready antes de la prueba.' }
$target = @($pods)[0].metadata.name
kubectl -n $ns delete pod $target --wait=false
if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar el Pod web seleccionado.' }
kubectl -n $ns get endpointslices -l kubernetes.io/service-name=cloudcolor-web
kubectl -n $ns rollout status deployment/cloudcolor-web --timeout=180s
if ($LASTEXITCODE -ne 0) { throw 'El despliegue no completo el rollout en el tiempo esperado.' }
kubectl -n $ns get pods -l app=cloudcolor-web -o wide
