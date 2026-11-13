$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
if ((kubectl config current-context) -ne 'cloudcolor') { throw 'El contexto activo debe ser cloudcolor.' }
kubectl --context cloudcolor get namespace cloudcolor -o name | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No existe el namespace cloudcolor.' }
$mkcert = Get-Command mkcert -ErrorAction SilentlyContinue
if ($mkcert) { $mkcertPath = $mkcert.Source } else { $mkcertPath = Join-Path $root '.local-certs/mkcert.exe' }
if (-not (Test-Path -LiteralPath $mkcertPath)) { throw 'Instale mkcert o coloque su ejecutable oficial en .local-certs.' }
$caDir = (& $mkcertPath -CAROOT).Trim()
$caFile = Join-Path $caDir 'rootCA.pem'
if (-not (Test-Path -LiteralPath $caFile)) { throw 'Primero cree e instale la CA local con mkcert -install.' }
$ca = [System.Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPem([IO.File]::ReadAllText($caFile))
$trusted = @(Get-ChildItem Cert:\CurrentUser\Root,Cert:\LocalMachine\Root | Where-Object Thumbprint -eq $ca.Thumbprint)
if ($trusted.Count -eq 0) { throw 'La CA local no esta en el almacen de confianza de Windows; instale la CA con mkcert -install.' }
$folder = Join-Path $root '.local-certs'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$cert = Join-Path $folder 'cloudcolor.local.pem'
$key = Join-Path $folder 'cloudcolor.local.key'
git check-ignore -q -- .local-certs/cloudcolor.local.key
if ($LASTEXITCODE -ne 0) { throw 'El directorio del certificado debe estar ignorado por Git.' }
if (-not (Test-Path -LiteralPath $cert) -or -not (Test-Path -LiteralPath $key)) {
    & $mkcertPath -cert-file $cert -key-file $key cloudcolor.local localhost 127.0.0.1
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el certificado local.' }
}
$leaf = [System.Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPem([IO.File]::ReadAllText($cert))
$san = $leaf.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.17' } | Select-Object -First 1
if ($leaf.NotAfter -le (Get-Date).AddDays(7) -or $leaf.Issuer -ne $ca.Subject -or
    $null -eq $san -or @($san.EnumerateDnsNames()) -notcontains 'cloudcolor.local') {
    throw 'El certificado local es incompatible o esta por vencer; revise los archivos locales.'
}
$controllers = (kubectl --context cloudcolor -n ingress-nginx get pods -l app.kubernetes.io/component=controller -o json | ConvertFrom-Json).items
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar ingress-nginx.' }
$ready = @($controllers | Where-Object { $_.status.phase -eq 'Running' -and @($_.status.conditions | Where-Object { $_.type -eq 'Ready' -and $_.status -eq 'True' }).Count -gt 0 })
if ($ready.Count -ne 1) { throw 'Se requiere un unico controlador Ingress listo en Minikube.' }
$address = [System.Net.IPAddress]::None
if (-not [System.Net.IPAddress]::TryParse($ready[0].status.podIP, [ref]$address) -or $address.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) { throw 'IP del controlador Ingress invalida.' }
$cidr = "$address/32"
$secret = kubectl --context cloudcolor -n cloudcolor create secret tls cloudcolor-local-tls --cert=$cert --key=$key --dry-run=client -o json
if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar el Secret TLS.' }
$secret | kubectl --context cloudcolor apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo aplicar el Secret TLS.' }
$deployment = kubectl --context cloudcolor -n cloudcolor get deployment cloudcolor-web -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el Deployment web.' }
$current = ($deployment.spec.template.spec.containers | Where-Object name -eq 'web').env | Where-Object name -eq 'CloudColor__TrustedProxyNetworks'
if ($current.value -ne $cidr) {
    kubectl --context cloudcolor -n cloudcolor set env deployment/cloudcolor-web "CloudColor__TrustedProxyNetworks=$cidr" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar el proxy confiable.' }
    kubectl --context cloudcolor -n cloudcolor rollout status deployment/cloudcolor-web --timeout=180s
    if ($LASTEXITCODE -ne 0) { throw 'El rollout web no termino correctamente.' }
}
kubectl --context cloudcolor -n cloudcolor patch ingress cloudcolor-web --type merge --patch-file k8s/overlays/local/ingress-tls-patch.yaml | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar TLS en Ingress.' }
for ($attempt=1; $attempt -le 30; $attempt++) {
    $ok = $true
    foreach ($path in @('/health/live','/health/ready','/Login')) {
        # La CA mkcert no publica CRL; Schannel conserva la validacion de cadena y nombre.
        $code = curl.exe --noproxy '*' --ssl-no-revoke --silent --output NUL --max-time 5 --write-out '%{http_code}' "https://cloudcolor.local$path"
        if ($LASTEXITCODE -ne 0 -or $code -ne '200') { $ok = $false; break }
    }
    $redirect = curl.exe --noproxy '*' --silent --output NUL --max-time 5 --write-out '%{http_code} %{redirect_url}' 'http://cloudcolor.local/'
    if ($LASTEXITCODE -ne 0 -or $redirect -notmatch '^30[1278] https://cloudcolor\.local/') { $ok = $false }
    if ($ok) { Write-Output "HTTPS y redireccion verificados; proxy confiable $cidr."; exit 0 }
    Start-Sleep -Seconds 2
}
throw 'HTTPS o redireccion no pasaron; revise el tunel Minikube y la confianza de la CA.'
