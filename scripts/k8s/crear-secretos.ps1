$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$envFile = Join-Path $root '.env'
if (-not (Test-Path -LiteralPath $envFile)) { throw 'No existe el archivo .env local.' }

# Compose interpreta el formato de .env; no se imprimen ni se guardan sus resultados.
$compose = [System.Diagnostics.ProcessStartInfo]::new()
$compose.FileName = (Get-Command docker -ErrorAction Stop).Source
$compose.WorkingDirectory = $root
$compose.UseShellExecute = $false
$compose.RedirectStandardOutput = $true
$compose.RedirectStandardError = $true
foreach ($arg in @('compose', '-f', (Join-Path $root 'compose.yaml'), '--env-file', $envFile, 'config', '--format', 'json')) { [void]$compose.ArgumentList.Add($arg) }
foreach ($key in @($compose.Environment.Keys)) {
    if ($key -eq 'MSSQL_SA_PASSWORD' -or $key -like 'CLOUDCOLOR_*') { [void]$compose.Environment.Remove($key) }
}
$process = [System.Diagnostics.Process]::Start($compose)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'No se pudo interpretar .env con Docker Compose; revise las variables requeridas.' }
$config = $stdout.GetAwaiter().GetResult() | ConvertFrom-Json
$sqlPassword = $config.services.sqlserver.environment.MSSQL_SA_PASSWORD
if ([string]::IsNullOrWhiteSpace($sqlPassword) -or $sqlPassword -cnotmatch '^[a-zA-Z0-9_!#%+@-]+$') {
    throw 'La clave SQL no cumple las restricciones documentadas para este entorno.'
}
$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder['Server'] = 'sqlserver,1433'
$builder['Database'] = 'CloudColorDB'
$builder['User ID'] = 'sa'
$builder['Password'] = $sqlPassword
$builder['Encrypt'] = 'True'
$builder['TrustServerCertificate'] = 'True'
$builder['Application Name'] = 'CloudColor.Web'
$values = @{ MSSQL_SA_PASSWORD = $sqlPassword; ConnectionStrings__CloudColor = $builder.ConnectionString }
foreach ($role in @('GERENTE', 'DIGITADOR', 'CAJERO')) {
    foreach ($field in @('CODIGO', 'NOMBRES', 'APELLIDOS', 'USUARIO', 'PASSWORD')) {
        $name = "CLOUDCOLOR_${role}_${field}"
        $value = $config.services.'gerente-init'.environment.$name
        if ([string]::IsNullOrWhiteSpace($value)) { throw "Falta la variable $name en .env." }
        $values[$name] = [string]$value
    }
}
$data = @{}
foreach ($name in $values.Keys) { $data[$name] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($values[$name])) }
$secret = @{ apiVersion = 'v1'; kind = 'Secret'; metadata = @{ name = 'cloudcolor-secrets'; namespace = 'cloudcolor' }; type = 'Opaque'; data = $data }
$json = $secret | ConvertTo-Json -Depth 8 -Compress
$kubectl = [System.Diagnostics.ProcessStartInfo]::new()
$kubectl.FileName = (Get-Command kubectl -ErrorAction Stop).Source
$kubectl.WorkingDirectory = $root
$kubectl.UseShellExecute = $false
$kubectl.RedirectStandardInput = $true
$kubectl.RedirectStandardOutput = $true
$kubectl.RedirectStandardError = $true
foreach ($arg in @('apply', '-f', '-')) { [void]$kubectl.ArgumentList.Add($arg) }
$process = [System.Diagnostics.Process]::Start($kubectl)
$outTask = $process.StandardOutput.ReadToEndAsync()
$errTask = $process.StandardError.ReadToEndAsync()
$process.StandardInput.Write($json)
$process.StandardInput.Close()
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'No se pudo crear el Secret; verifique el contexto, permisos y namespace cloudcolor.' }
Write-Output 'Secret cloudcolor-secrets creado o actualizado.'
