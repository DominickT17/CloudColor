$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'utilidades.ps1')
Assert-CloudColorContext
Write-Output '--- Nodo ---'
kubectl --context cloudcolor get nodes -o wide
Write-Output '--- Deployment y Pods ---'
kubectl --context cloudcolor -n cloudcolor get deployment,pods -o wide
Write-Output '--- CPU y memoria ---'
kubectl --context cloudcolor top nodes
if ($LASTEXITCODE -ne 0) { Write-Output 'Metricas de nodo no disponibles todavia.' }
kubectl --context cloudcolor -n cloudcolor top pods
if ($LASTEXITCODE -ne 0) { Write-Output 'Metricas de Pods no disponibles todavia.' }
Write-Output '--- Service, endpoints e Ingress ---'
kubectl --context cloudcolor -n cloudcolor get services,endpointslices,ingresses -o wide
Write-Output '--- PVC y Jobs ---'
kubectl --context cloudcolor -n cloudcolor get pvc,jobs
Write-Output '--- Eventos recientes (sin mensajes) ---'
$events = kubectl --context cloudcolor -n cloudcolor get events --sort-by=.metadata.creationTimestamp -o 'custom-columns=TIPO:.type,RAZON:.reason,OBJETO:.involvedObject.name' --no-headers
$events | Select-Object -Last 10
Write-Output '--- Health por Service ---'
Test-CloudColorInternalHealth
