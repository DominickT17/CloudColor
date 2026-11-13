# CloudColor con Dokploy y Docker Compose

Esta guía prepara un despliegue futuro. No se ha creado ningún servidor, dominio, certificado ni instancia Dokploy. El Compose de esta carpeta lógica es `compose.production.yaml`, en la raíz del repositorio.

## Arquitectura

```text
Internet → DNS → HTTPS → Traefik/Dokploy → web:8080
                                               ↓
                                          sqlserver:1433
                                               ↓
                                    volumen cloudcolor_sql_data

GitHub → GitHub Actions → Docker Hub → API Dokploy → Compose
```

`sqlserver` no publica puertos al host. Los inicializadores y la web se comunican por la red `cloudcolor`. Solo la web debe ser conectada a Traefik. El Compose usa imágenes publicadas; no contiene `build`. SQL usa `ACCEPT_EULA=Y`, edición Developer, healthcheck y volumen nombrado. `sql-init` espera a SQL; `user-init` espera la instalación; la web espera a que ambos terminen. Los dos inicializadores son one-shot y su esquema se migra de versión 1 a 2 según `docker/sql/init.sh`. La versión 2 vuelve a comprobar la migración sin recrear las tablas. El script 12 de datos de demostración no se ejecuta.

`pull_policy: always` solicita las imágenes en cada despliegue y los logs usan `json-file` con rotación de tres archivos de 10 MB por contenedor. Confirme también en Dokploy la opción de descargar las imágenes en cada despliegue y revise el log de Compose: un `latest` previo en el servidor no demuestra que corra la imagen nueva. Si usa un Run Command personalizado, se reemplaza el comando predeterminado de Dokploy; no quite accidentalmente la descarga ni agregue opciones de borrado de volúmenes.

## Requisitos y configuración en Dokploy

1. Prepare un VPS Linux x86-64 con recursos suficientes para SQL Server, almacenamiento persistente, Docker Compose moderno y Dokploy. La edición SQL Developer es exclusivamente para desarrollo/pruebas; una implementación comercial requiere edición y licencias adecuadas.
2. En Dokploy cree un **Project**, un **Environment** y un servicio **Docker Compose** de tipo Docker Compose, no Stack.
3. Conecte GitHub y seleccione `DominickT17/CloudColor`, rama `redes/proyecto-final`, con ruta `compose.production.yaml`. La revisión que contenga estos archivos y las tres imágenes debe estar publicada antes del despliegue.
4. Configure en el editor de variables de Dokploy las variables de `.env.production.example`: contraseña SQL, los cinco campos de cada cuenta, las tres etiquetas, el dominio y `DOKPLOY_PROXY_CIDR`. No copie credenciales al repositorio. Las variables de Compose deben estar disponibles en el `.env` que Dokploy genera para la ruta del Compose.
5. Configure un dominio en la sección Domains del Compose, seleccionando **web** y puerto **8080**. Dokploy añade las etiquetas y conexión de proxy durante el despliegue. Use **Preview Compose** para verificar que web conserve la red privada `cloudcolor` hacia SQL y tenga además la red compartida con Traefik; SQL e inicializadores no necesitan pertenecer a la red del proxy. La implementación puede variar con la versión y el modo aislado de Dokploy: valide el Compose generado antes de aplicarlo. [Dominios de Compose en Dokploy](https://docs.dokploy.com/docs/core/docker-compose/domains).
6. Configure DNS hacia el VPS, habilite HTTPS y redirección HTTP→HTTPS en Dokploy/Traefik, y realice el despliegue. Los cambios de dominio requieren redeploy. No se proporciona un dominio real ni certificados en el repositorio.
7. Revise el resultado de ambos inicializadores y compruebe `/health/live`, `/health/ready` y `/Login` por HTTPS. Consulte [persistencia](PERSISTENCIA.md), [monitoreo](../monitoring/README.md) y [hot update](HOT-UPDATE.md).

`DOMINIO_CLOUDCOLOR` alimenta `AllowedHosts`: debe ser el host real sin protocolo, puerto, ruta ni comodines. Los health checks se exponen por ese host. El puerto 8080 solo se expone a las redes Docker, no al host. El contenedor ASP.NET no incluye `curl` ni `wget`, por lo que no se añadió un healthcheck Docker web que falle por falta de herramientas. Use los endpoints mediante Traefik/Dokploy o el smoke test. `live` comprueba proceso; `ready` comprueba SQL y versión 2.

Las etiquetas por defecto son `latest`, `db-init-latest` y `user-init-latest`. Para un lanzamiento reproducible, cambie las tres variables de imagen a los tags SHA de la misma revisión y verifique su existencia. El flujo automático de esta fase usa los valores configurados en Dokploy: si se fijan tags SHA, un redeploy por sí solo **no los actualiza**.

## Contraseñas y Data Protection

El inicializador SQL actual solo admite para `MSSQL_SA_PASSWORD` letras ASCII, dígitos y `_!#%+@-`; además debe cumplir la política de SQL Server. Esta restricción evita ambigüedades en la cadena construida por Compose. Las contraseñas de las cuentas nuevas deben tener entre 12 y 128 caracteres. Se requieren los datos de las tres cuentas aun cuando ya existan; cambiar esas variables no cambia las cuentas existentes. Si los datos del Gerente no permiten autenticarse, o las cuentas no corresponden al rol/empleado esperado, la inicialización falla sin modificarlas.

`CloudColor__UseSqlDataProtection=true` guarda las claves compartidas en SQL, por lo que no dependen del sistema de archivos del contenedor web. Esas claves están **sin cifrado adicional en la tabla**; antes de producción real configure protección en reposo, respaldos y permisos mínimos. La demostración aún usa `sa`, `TrustServerCertificate=True` y variables de entorno; debe sustituirse por una cuenta SQL con privilegios mínimos, TLS verificable y un gestor de secretos apropiado. No imprima `docker compose config` con variables reales: muestra secretos interpolados. `.env.production` y `.env.production.local` están ignorados por Git.

## Proxy confiable

En el servidor, `deploy/dokploy/detectar-red-proxy.sh` solo consulta redes y subredes de Traefik. Compare su salida con el **Preview Compose** y verifique la IP de origen que ASP.NET ve realmente; NAT y la topología pueden hacer que una subred listada no sea la fuente efectiva. Configure `DOKPLOY_PROXY_CIDR` con la red precisa del proxy confiable; se permiten varios CIDR separados por `;`. No utilice `0.0.0.0/0` ni confíe en redes con contenedores no confiables. Se necesita la configuración correcta para respetar `X-Forwarded-Proto`, las cookies seguras y evitar redirecciones incorrectas. La configuración base no inventa una red Docker de Dokploy.

## CI y CD

GitHub Actions publica primero las tres imágenes. Después, si `DOKPLOY_URL`, `DOKPLOY_COMPOSE_ID` y el secreto `DOKPLOY_API_KEY` están configurados en GitHub, llama `POST /api/compose.redeploy` con `freshVolumes=false`. Si falta cualquiera, muestra que el despliegue está pendiente y termina sin llamar a Dokploy. `CLOUDCOLOR_PUBLIC_URL` es opcional y habilita el smoke test. Use HTTPS para la API y conceda al token permisos de despliegue y lectura del historial necesarios para ese Compose.

La API encola el trabajo: una respuesta HTTP exitosa no significa que el despliegue haya terminado. El workflow identifica su ejecución por título y consulta `deployment.allByCompose` hasta `done`, `error`, `cancelled` o timeout; no imprime el cuerpo ni las credenciales. Si el despliegue está todavía en curso o el resultado es ambiguo, falla y requiere revisión antes de reintentar. [API de Compose](https://docs.dokploy.com/docs/api/compose) y [API de despliegues](https://docs.dokploy.com/docs/api/deployment).

En el código actual de Dokploy, `compose.redeploy` reutiliza el checkout existente y no vuelve a clonar la fuente Git. Por ello sirve para actualizar las imágenes de un Compose ya instalado, pero **no garantiza aplicar cambios nuevos al propio archivo Compose**. Para cambios de configuración, realice un Deploy que actualice la fuente y verifique la versión instalada antes de habilitar otra vez el CD. Confirme este comportamiento con la versión real de Dokploy. [Implementación de Dokploy](https://github.com/Dokploy/dokploy/blob/main/packages/server/src/services/compose.ts).

Desactive el Auto Deploy nativo del proveedor Git en Dokploy: puede adelantarse a la publicación de imágenes. La concurrencia de Actions encola las ejecuciones y no cancela una activa, pero no controla despliegues manuales, otros workflows, reintentos tras timeout ni garantiza el orden de envío de todos los pushes. Tampoco fija la revisión del checkout que usa el redeploy. Para trazabilidad estricta use imágenes y fuente fijadas por SHA y una coordinación de releases; confirme el digest y el contenido realmente servido. Un smoke test 200 por sí solo no demuestra que se desplegó el commit nuevo.

## Operación y rollback

La actualización convencional de un único contenedor web con Compose puede interrumpir tráfico mientras se recrea. No se garantiza cero downtime. Dokploy documenta para **Applications** un mecanismo basado en salud y opciones Swarm; no es una garantía automática para este Compose. Para cero downtime estricto se necesitaría diseñar y probar réplicas, health routing, actualización `start-first`, capacidad y compatibilidad de migraciones en una arquitectura soportada, posiblemente Stack/Swarm. No se activa aquí. [Guía de Dokploy](https://docs.dokploy.com/docs/core/applications/zero-downtime).

Un rollback conceptual consiste en seleccionar tags SHA conocidos de las tres imágenes y redeployar sin borrar volúmenes, después de confirmar que la versión anterior es compatible con el esquema SQL actual. Una imagen anterior no revierte automáticamente una migración de base; pruebe restauración de respaldos antes de producción. Fail2ban SSH y el diagnóstico del servidor están documentados en [deploy/server](../server/README.md).
