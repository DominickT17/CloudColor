param([Parameter(Mandatory=$true)][string]$Url)
$ErrorActionPreference = 'Stop'
try {
    $uri = [Uri]$Url
    if ($uri.Scheme -notin @('http','https') -or -not $uri.Host -or $uri.UserInfo -or $uri.Query -or $uri.Fragment) { throw 'URL invalida.' }
    $base = $Url.TrimEnd('/')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(10)
    foreach ($path in @('/health/live','/health/ready','/Login')) {
        $success = $false
        for ($attempt=1; $attempt -le 20; $attempt++) {
            try {
                $response = $client.GetAsync($base + $path).GetAwaiter().GetResult()
                $success = [int]$response.StatusCode -eq 200
                $response.Dispose()
            } catch { $success = $false }
            if ($success) { break }
            if ($attempt -lt 20) { Start-Sleep -Seconds 5 }
        }
        if (-not $success) { throw "$path no devolvio HTTP 200." }
        Write-Output "$path`: HTTP 200"
    }
    $client.Dispose()
} catch {
    Write-Error 'Smoke test fallido; revise la URL y el estado del servicio.'
    exit 1
}
