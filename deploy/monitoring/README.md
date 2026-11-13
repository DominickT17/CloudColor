# Monitoreo básico

En Dokploy revise y registre evidencia de estado de web y SQL Server, CPU, RAM, tiempo en ejecución y logs. Ejecute los smoke tests contra `/health/live`, `/health/ready` y `/Login`; `live` solo comprueba proceso, `ready` también SQL y esquema. Compruebe los logs de `sql-init` y `user-init` ante un fallo de arranque, sin capturar variables de entorno ni credenciales.

En el VPS, dentro del directorio/configuración Compose correspondiente:

```bash
docker stats --no-stream
docker compose -f compose.production.yaml ps
docker compose -f compose.production.yaml logs --tail=100
```

La rotación del driver `json-file` limita los logs de los contenedores a tres archivos de 10 MB cada uno; los logs de Dokploy y del sistema pueden tener políticas distintas. Prometheus y Grafana no se instalan ni son necesarios para este despliegue. Una integración opcional posterior debería usar monitoreo externo, retención y acceso autenticado; no se ha preparado un stack adicional sin conocer el VPS.
