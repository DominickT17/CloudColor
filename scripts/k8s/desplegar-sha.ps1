param([Parameter(Mandatory=$true)][string]$Sha)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'utilidades.ps1')
if ($Sha -cnotmatch '^[0-9a-f]{40}$') { throw 'El SHA debe contener 40 caracteres hexadecimales minusculos.' }
Assert-CloudColorContext
$state = Assert-WebAvailable
if ($state.Deployment.spec.strategy.rollingUpdate.maxUnavailable -ne 0 -or $state.Deployment.spec.strategy.rollingUpdate.maxSurge -ne 1) { throw 'La estrategia RollingUpdate no cumple las protecciones requeridas.' }
$image = "dominickt1703/cloudcolor-redesii:sha-$Sha"
$current = ($state.Deployment.spec.template.spec.containers | Where-Object name -eq 'web').image
if ($current -ne $image) {
    kubectl --context cloudcolor -n cloudcolor set image deployment/cloudcolor-web "web=$image"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo actualizar la imagen.' }
}
kubectl --context cloudcolor -n cloudcolor rollout status deployment/cloudcolor-web --timeout=180s
if ($LASTEXITCODE -ne 0) { throw 'El rollout no termino correctamente.' }
$state = Assert-WebAvailable
if (($state.Deployment.spec.template.spec.containers | Where-Object name -eq 'web').image -ne $image) { throw 'La imagen desplegada no coincide con el SHA.' }
Test-CloudColorInternalHealth
Write-Output "Imagen desplegada: $image"
kubectl --context cloudcolor -n cloudcolor get deployment cloudcolor-web
kubectl --context cloudcolor -n cloudcolor get pods -l app=cloudcolor-web -o wide
kubectl --context cloudcolor -n cloudcolor get endpointslices -l kubernetes.io/service-name=cloudcolor-web
