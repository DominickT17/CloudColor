function Assert-CloudColorContext {
    if ((kubectl config current-context) -ne 'cloudcolor') { throw 'El contexto activo debe ser cloudcolor.' }
    $status = minikube status -p cloudcolor -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $status.Host -ne 'Running' -or $status.Kubelet -ne 'Running' -or $status.APIServer -ne 'Running') { throw 'Minikube cloudcolor no esta listo.' }
    kubectl --context cloudcolor get namespace cloudcolor -o name | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No existe el namespace cloudcolor.' }
}

function Get-WebState {
    $d = kubectl --context cloudcolor -n cloudcolor get deployment cloudcolor-web -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el Deployment.' }
    $pods = kubectl --context cloudcolor -n cloudcolor get pods -l app=cloudcolor-web -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los Pods.' }
    $slices = kubectl --context cloudcolor -n cloudcolor get endpointslices -l kubernetes.io/service-name=cloudcolor-web -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los endpoints.' }
    $readyPods = @($pods.items | Where-Object { -not $_.metadata.deletionTimestamp -and $_.status.phase -eq 'Running' -and @($_.status.conditions | Where-Object { $_.type -eq 'Ready' -and $_.status -eq 'True' }).Count -gt 0 })
    $endpoints = @($slices.items | ForEach-Object { $_.endpoints } | Where-Object { $_.conditions.ready -eq $true })
    [PSCustomObject]@{ Deployment=$d; Pods=$readyPods; Endpoints=$endpoints }
}

function Assert-WebAvailable {
    $state = Get-WebState
    if ($state.Deployment.spec.replicas -lt 2 -or $state.Deployment.status.availableReplicas -lt 2 -or $state.Pods.Count -lt 2 -or $state.Endpoints.Count -lt 2) { throw 'La web no tiene dos Pods y endpoints disponibles.' }
    return $state
}

function Start-CloudColorForward([string]$Namespace, [string]$Resource, [int]$TargetPort) {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = (Get-Command kubectl -ErrorAction Stop).Source
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    foreach ($argument in @('--context','cloudcolor','-n',$Namespace,'port-forward','--address','127.0.0.1',$Resource,"${port}:${TargetPort}")) { [void]$info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($info)
    [void]$process.StandardOutput.ReadToEndAsync(); [void]$process.StandardError.ReadToEndAsync()
    for ($i=0; $i -lt 60; $i++) {
        if ($process.HasExited) { throw 'El port-forward termino antes de estar listo.' }
        $client = [Net.Sockets.TcpClient]::new()
        try { $task=$client.ConnectAsync('127.0.0.1',$port); if ($task.Wait(200) -and $client.Connected) { return [PSCustomObject]@{Process=$process;Port=$port} } } catch {} finally { $client.Dispose() }
        Start-Sleep -Milliseconds 250
    }
    if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
    throw 'El port-forward no estuvo disponible.'
}

function Stop-CloudColorForward($Forward) {
    if ($null -ne $Forward) { if (-not $Forward.Process.HasExited) { $Forward.Process.Kill($true); $Forward.Process.WaitForExit() }; $Forward.Process.Dispose() }
}

function Test-CloudColorInternalHealth {
    $forward = $null
    try {
        $forward = Start-CloudColorForward 'cloudcolor' 'service/cloudcolor-web' 80
        foreach ($path in @('/health/live','/health/ready')) {
            $code = curl.exe --noproxy '*' --silent --output NUL --max-time 5 --write-out '%{http_code}' "http://127.0.0.1:$($forward.Port)$path"
            if ($LASTEXITCODE -ne 0 -or $code -ne '200') { throw "$path no devolvio HTTP 200." }
            Write-Output "$path`: HTTP 200"
        }
    } finally { Stop-CloudColorForward $forward }
}

function Start-CloudColorMonitor {
    $forward = Start-CloudColorForward 'ingress-nginx' 'service/ingress-nginx-controller' 443
    try {
        $job = Start-Job -ArgumentList $forward.Port -ScriptBlock {
            param($Port)
            while ($true) {
                $code = curl.exe --noproxy '*' --ssl-no-revoke --silent --output NUL --max-time 4 --resolve "cloudcolor.local:${Port}:127.0.0.1" --write-out '%{http_code}' "https://cloudcolor.local:${Port}/health/ready" 2>$null
                if ($LASTEXITCODE -ne 0) { $code='000' }
                Write-Output $code
                Start-Sleep -Milliseconds 400
            }
        }
        Start-Sleep -Seconds 2
        return [PSCustomObject]@{Forward=$forward;Job=$job}
    } catch { Stop-CloudColorForward $forward; throw }
}

function Stop-CloudColorMonitor($Monitor) {
    if ($null -eq $Monitor) { return }
    try {
        Stop-Job -Job $Monitor.Job -ErrorAction SilentlyContinue
        $samples = @(Receive-Job -Job $Monitor.Job -ErrorAction SilentlyContinue)
        Remove-Job -Job $Monitor.Job -Force -ErrorAction SilentlyContinue
        $ok = @($samples | Where-Object { $_ -eq '200' }).Count
        return [PSCustomObject]@{Total=$samples.Count;Success=$ok;Failures=($samples.Count-$ok)}
    } finally { Stop-CloudColorForward $Monitor.Forward }
}
