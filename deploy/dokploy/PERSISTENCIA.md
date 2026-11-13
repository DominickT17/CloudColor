# Persistencia de CloudColor

SQL Server guarda bases, logs y claves de Data Protection bajo `/var/opt/mssql`, en el volumen Docker nombrado `cloudcolor_sql_data`. El Compose establece ese nombre de forma explícita; no comparta accidentalmente el mismo volumen entre proyectos o entornos. Verifique el nombre y la ubicación reales en Dokploy antes de operar.

Un redeploy normal o la recreación de un contenedor conservan el volumen. **No ejecute `docker compose down -v` en producción** ni seleccione Fresh Volumes: ambas acciones pueden eliminar datos. El API de este proyecto fija `freshVolumes=false`. El volumen no es un backup; prepare copias SQL consistentes, copia externa cifrada y pruebas de restauración antes de uso real. Las claves de Data Protection son necesarias para leer cookies existentes y deben protegerse junto con la base.

En el servidor, desde el proyecto Compose y con sus variables disponibles, estas consultas son de lectura:

```bash
docker volume inspect cloudcolor_sql_data
docker compose -f compose.production.yaml ps
docker compose -f compose.production.yaml exec -T sqlserver /bin/bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -C -b -d master -Q "SET NOCOUNT ON; SELECT name, state_desc FROM sys.databases WHERE name = '\''CloudColorDB'\'';"'
docker compose -f compose.production.yaml exec -T sqlserver /bin/bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -C -b -d CloudColorDB -Q "SET NOCOUNT ON; SELECT CONVERT(nvarchar(10), value) AS Version FROM sys.extended_properties WHERE class=0 AND name=N'\''CloudColorDockerSchemaVersion'\''; SELECT r.Nombre AS Rol, COUNT(*) AS Usuarios FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID=u.RolID GROUP BY r.Nombre; SELECT COUNT(*) AS Claves FROM dbo.AspNetDataProtectionKeys;"'
```

No muestran contraseñas, nombres de usuario ni XML de claves. La contraseña SQL usada en la inicialización de un volumen queda asociada a esa instancia: cambiar la variable en Dokploy **no cambia** por sí mismo la contraseña de SQL Server. Si el esquema es anterior o la instalación está incompleta, revise logs y respaldos; no borre la base para reintentar.
