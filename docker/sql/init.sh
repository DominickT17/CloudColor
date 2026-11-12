#!/usr/bin/env bash
set -euo pipefail

# La cadena de conexion de Compose inserta esta contrasena sin comillas.
if [[ ! "$SQLCMDPASSWORD" =~ ^[a-zA-Z0-9_!#%+@-]+$ ]]; then
  echo 'MSSQL_SA_PASSWORD contiene caracteres no admitidos; consulte .env.example.' >&2
  exit 1
fi

sqlcmd=(/opt/mssql-tools18/bin/sqlcmd -S sqlserver,1433 -U sa -C -b -l 3)
echo 'Esperando a SQL Server...'
ready=0
for ((attempt=1; attempt<=60; attempt++)); do
  if "${sqlcmd[@]}" -Q 'SELECT 1' >/dev/null 2>&1; then ready=1; break; fi
  sleep 2
done
if (( ready == 0 )); then
  echo 'SQL Server no estuvo disponible o las credenciales no son validas.' >&2
  exit 1
fi

exists=$("${sqlcmd[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('CloudColorDB') IS NULL THEN 0 ELSE 1 END;" | tr -d '[:space:]')
if [[ "$exists" == 1 ]]; then
  echo 'Esperando a que CloudColorDB termine su recuperacion...'
  database_ready=0
  for ((attempt=1; attempt<=60; attempt++)); do
    if state=$("${sqlcmd[@]}" -d master -h -1 -W -t 3 -Q "SET NOCOUNT ON; SELECT state_desc FROM sys.databases WHERE name = N'CloudColorDB';" 2>/dev/null | tr -d '[:space:]'); then
      if [[ "$state" == ONLINE ]] && "${sqlcmd[@]}" -d CloudColorDB -t 3 -Q 'SELECT 1' >/dev/null 2>&1; then
        database_ready=1
        break
      fi
    fi
    sleep 2
  done
  if (( database_ready == 0 )); then
    echo 'CloudColorDB no estuvo ONLINE y accesible dentro del tiempo de espera.' >&2
    exit 1
  fi
  marker=$("${sqlcmd[@]}" -d CloudColorDB -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'CloudColorDockerSchemaVersion' AND CONVERT(nvarchar(10), value) = N'1') THEN 1 ELSE 0 END;" | tr -d '[:space:]')
  if [[ "$marker" != 1 ]]; then
    echo 'CloudColorDB existe sin marca de instalacion completa. Detenido; revise la base manualmente.' >&2
    exit 1
  fi
  echo 'CloudColorDB ya tiene la marca de instalacion completa; no se repiten scripts.'
  exit 0
fi
if [[ "$exists" != 0 ]]; then
  echo 'No se pudo determinar el estado de CloudColorDB.' >&2
  exit 1
fi

scripts=(
  00_CreateDatabase.sql 01_CreateTables.sql 02_InitialData.sql
  03_Procedures_Fase1.sql 04_Functions_Fase1.sql 05_Views_Fase1.sql
  06_Types_Fase2.sql 07_Views_Fase2.sql 08_Procedures_Catalogos_Fase2.sql
  09_Procedures_Cotizacion.sql 10_Procedures_Facturacion.sql 11_Procedures_Compras.sql
  13_Types_ProyectoFinal.sql 14_Views_ProyectoFinal.sql 15_Procedures_ProyectoFinal.sql
  16_Sincronizar_Procedimientos.sql
)
for script in "${scripts[@]}"; do
  echo "Ejecutando $script"
  "${sqlcmd[@]}" -i "/scripts/$script"
done
"${sqlcmd[@]}" -d CloudColorDB -Q "EXEC sys.sp_addextendedproperty @name=N'CloudColorDockerSchemaVersion', @value=N'1';"
echo 'Instalacion SQL completada.'
