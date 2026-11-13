$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'utilidades.ps1')
Assert-CloudColorContext
Write-Output '--- Contexto ---'
kubectl config current-context
& (Join-Path $PSScriptRoot 'monitorear-cloudcolor.ps1')
Write-Output '--- TLS de Ingress ---'
$ingress = kubectl --context cloudcolor -n cloudcolor get ingress cloudcolor-web -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar Ingress.' }
Write-Output "Host: $($ingress.spec.rules[0].host); Secret TLS: $($ingress.spec.tls[0].secretName)"
$secretType = kubectl --context cloudcolor -n cloudcolor get secret cloudcolor-local-tls -o 'jsonpath={.type}'
if ($LASTEXITCODE -ne 0 -or $secretType -ne 'kubernetes.io/tls') { throw 'El Secret TLS no esta disponible.' }
Write-Output "Tipo TLS: $secretType"
$state=Get-WebState
Write-Output "Imagen: $(($state.Deployment.spec.template.spec.containers | Where-Object name -eq 'web').image)"
