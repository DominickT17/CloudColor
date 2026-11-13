# CloudColor en Kubernetes

Esta carpeta prepara un despliegue; no instala ni configura un clúster. La alta disponibilidad corresponde a la **web**, no a SQL Server. SQL Server tiene una réplica y almacenamiento persistente: una caída de esa instancia o del almacenamiento puede dejar la aplicación no disponible. El PDB protege frente a ciertas interrupciones voluntarias, no frente a fallos simultáneos ni eliminaciones directas de Pods.

## Arquitectura

```text
Internet / navegador
        ↓
Ingress NGINX → Service cloudcolor-web:80
        ↓
   Web Pod 1     Web Pod 2
        └──────┬──────┘
               ↓
  Service sqlserver:1433 (ClusterIP)
               ↓
  StatefulSet SQL Server (1 Pod)
               ↓
       PVC de 5 GiB
```

Los Jobs `sql-init` y `user-init` preparan respectivamente el esquema y las cuentas. La web emplea la conexión del Secret y comparte claves de Data Protection en SQL. Las claves permiten que una cookie de un Pod se lea en el otro; las sesiones anteriores a la configuración compartida pueden dejar de ser válidas. Las claves se guardan **sin cifrado adicional en la tabla**: antes de producción se deben proteger en reposo y limitar estrictamente quién puede leer o modificar la base y sus respaldos. `sa`, `TrustServerCertificate=True` y SQL Server Developer son opciones de esta demostración, no una configuración de producción. Developer no es una licencia para producción.

Imágenes desde Docker Hub:

- Web: `dominickt1703/cloudcolor-redesii:latest` o `sha-<commit>`.
- SQL init: `dominickt1703/cloudcolor-redesii:db-init-latest` o `db-init-sha-<commit>`.
- Usuarios: `dominickt1703/cloudcolor-redesii:user-init-latest` o `user-init-sha-<commit>`.
- SQL Server: imagen oficial `mcr.microsoft.com/mssql/server:2022-latest`.

El pipeline construye las tres primeras imágenes después de un push a la rama configurada. Las etiquetas SHA se basan en el SHA completo del commit. Deben existir en Docker Hub antes de desplegar.

## Preparación local con Minikube

Se necesita Minikube ya instalado, Docker, `kubectl`, PowerShell y recursos suficientes: SQL Server solicita 2 GiB y puede usar hasta 4 GiB, además de la web y los componentes del clúster. El clúster debe contar con una StorageClass predeterminada que admita `ReadWriteOnce`. El StatefulSet conserva el PVC al recrear su Pod; un PVC no sustituye respaldos y su disponibilidad depende del proveedor de almacenamiento.

Desde la raíz del clon, para una instalación nueva, habilite el controlador Ingress y cree primero el namespace. Compruebe previamente que el contexto activo sea `cloudcolor`:

```powershell
kubectl config current-context
minikube addons enable ingress -p cloudcolor
kubectl apply -f k8s/base/namespace.yaml
.\scripts\k8s\crear-secretos.ps1
kubectl apply -k k8s/overlays/local
```

El script de secretos interpreta el `.env` existente mediante Compose y envía el Secret `cloudcolor-secrets` a `kubectl` por entrada estándar; no escribe un archivo temporal con sus valores. No lo ejecute hasta comprobar el contexto de `kubectl` y los permisos. Las variables de SQL y de las tres cuentas deben existir en `.env`. El archivo sigue ignorado por Git. Los Secrets de Kubernetes están codificados, no cifrados por defecto: configure cifrado de etcd y RBAC en un clúster real. El usuario de SQL utilizado actualmente por los tres componentes es `sa`.

El overlay local usa `Development` y HTTPS local; no lo use para producción. Configure `cloudcolor.local` en hosts de Windows y mantenga activo `minikube tunnel -p cloudcolor` cuando el controlador lo requiera. Instale el binario oficial de mkcert, ejecute `mkcert -install` y apruebe la confianza de la CA en Windows cuando se solicite. La CA privada permanece fuera del repositorio; el certificado y la clave local se guardan en `.local-certs/`, ignorado por Git. El script también acepta el ejecutable oficial `.local-certs/mkcert.exe`:

```powershell
.\scripts\k8s\configurar-https-local.ps1
```

El script comprueba contexto y CA, crea o reutiliza el certificado, actualiza el Secret TLS y configura como proxy confiable únicamente la IP `/32` del controlador Ingress listo. Si el Pod Ingress cambia de IP, vuelva a ejecutarlo; también comprueba HTTPS y redirección. En este equipo `curl.exe` con Schannel necesita `--ssl-no-revoke` porque la CA local no publica una CRL: se siguen validando la cadena y el nombre, sin `-k`. El navegador o un cliente .NET con el almacén de confianza de Windows pueden validar el certificado normalmente.

**No reaplique el overlay completo durante una actualización normal:** su Deployment base puede sustituir la imagen SHA y el proxy `/32` configurados en vivo. Para TLS use el script anterior y para actualizar la web use `desplegar-sha.ps1`; las migraciones y los Jobs se gestionan por separado.

Compruebe que ambos Jobs terminen antes de usar la aplicación:

```powershell
kubectl -n cloudcolor get pods
kubectl -n cloudcolor get svc
kubectl -n cloudcolor get ingress
kubectl -n cloudcolor get jobs
kubectl -n cloudcolor get pvc
kubectl -n cloudcolor wait --for=condition=complete job/sql-init --timeout=600s
kubectl -n cloudcolor wait --for=condition=complete job/user-init --timeout=600s
kubectl -n cloudcolor logs job/sql-init
kubectl -n cloudcolor logs job/user-init
kubectl -n cloudcolor rollout status deployment/cloudcolor-web --timeout=300s
```

`sql-init` espera SQL Server y su recuperación, instala `00–11` y `13–16` únicamente en una base nueva, y aplica la migración `17`. Una base marcada `1` pasa a `2`; una marcada `2` se comprueba sin recrear tablas. Una marca ausente o desconocida detiene la instalación. Una instalación inicial interrumpida antes de registrar la marca requiere revisión manual. No se ejecuta el script `12`. El Job de usuarios espera el esquema 2 y conserva las cuentas existentes si coinciden; autentica el Gerente configurado. Si los datos son incompatibles, termina con error y revierte sus altas.

Los Jobs tienen nombres fijos y Kubernetes no vuelve a ejecutar automáticamente un Job completado al reaplicar los manifiestos. Para una nueva versión con migraciones, planifique una ejecución nueva antes de actualizar la web. Tras verificar el estado y los respaldos, puede eliminar **solo los objetos Job** `sql-init` y `user-init` y volver a aplicar el overlay para recrearlos; nunca elimine el StatefulSet ni el PVC como mecanismo de migración. Las especificaciones de Job son inmutables, por lo que actualizar sus imágenes también requiere recrear esos Jobs. Evite ejecutar varias instalaciones iniciales simultáneas contra una base inexistente.

## Probes y demostración de disponibilidad

Con el Ingress local operativo:

```powershell
curl.exe --ssl-no-revoke -i https://cloudcolor.local/health/live
curl.exe --ssl-no-revoke -i https://cloudcolor.local/health/ready
kubectl -n cloudcolor get pods -l app=cloudcolor-web -o wide
kubectl -n cloudcolor get endpointslices -l kubernetes.io/service-name=cloudcolor-web
```

`/health/live` verifica el proceso sin consultar SQL. `/health/ready` comprueba conexión y esquema `2`; devuelve 503 si no puede utilizarlos. Los probes no revelan credenciales ni detalles de excepciones y no necesitan autenticación. Un fallo SQL retira las réplicas del Service sin hacer fallar su liveness.

Para probar el reemplazo de un único Pod web, con al menos dos Pods `Ready`:

```powershell
.\scripts\k8s\probar-alta-disponibilidad.ps1
.\scripts\k8s\probar-alta-disponibilidad.ps1 -EliminarUnPod
kubectl -n cloudcolor get pods -l app=cloudcolor-web -w
```

El script solo selecciona Pods web. Verifique los EndpointSlices durante la prueba y que el navegador siga respondiendo. Inicie sesión antes de eliminar el Pod, mantenga la cookie y compruebe las solicitudes posteriores en otra réplica; esta prueba debe realizarse de forma efectiva, el manifiesto por sí solo no demuestra HA. Con un solo nodo Minikube ambos Pods pueden compartir nodo, por lo que su caída afectaría a ambos. La afinidad preferida intenta separarlos cuando hay varios nodos.

## Preparación de producción y rollout

`k8s/overlays/production/kustomization.yaml` contiene tres etiquetas `sha-REEMPLAZAR` y el dominio reservado `cloudcolor.tu-dominio.invalid`. Sustituya los tres SHA por el mismo commit ya publicado y cambie **ambas** apariciones del dominio antes del despliegue. El manifiesto TLS referencia el Secret `cloudcolor-tls` y el `ClusterIssuer` de ejemplo `letsencrypt-prod`. Se necesitarán un dominio real, DNS, NGINX Ingress, cert-manager, un ClusterIssuer funcional y certificados; nada de eso se instala aquí.

Antes de habilitar HTTPS, configure en el Deployment `CloudColor__TrustedProxyNetworks` con los CIDR reales y restringidos de los proxies que conectan directamente a los Pods, separados por `;`. El valor base `127.0.0.1/32` solo confía en loopback y es un marcador seguro: no confíe en toda la red ni exponga el Service directamente a clientes que puedan falsificar encabezados. `UseForwardedHeaders` procesa `X-Forwarded-For` y `X-Forwarded-Proto` antes de HTTPS. Un proxy no confiable puede causar redirecciones o cookies incorrectas. Ajuste la red, el número de saltos y las políticas de red a la topología real.

Para cambiar de versión, publique primero las tres imágenes SHA, revise la compatibilidad y los respaldos, ejecute el Job SQL de esa versión y espere su éxito, luego el Job de usuarios si corresponde y finalmente actualice la imagen web. El Deployment utiliza `maxUnavailable: 0`, `maxSurge: 1` y readiness; esto minimiza interrupciones pero depende de capacidad, almacenamiento y una migración compatible con la versión anterior. Compruebe:

```powershell
kubectl -n cloudcolor rollout status deployment/cloudcolor-web --timeout=300s
kubectl -n cloudcolor rollout history deployment/cloudcolor-web
kubectl -n cloudcolor get pods -l app=cloudcolor-web -o wide
```

La StorageClass puede configurarse mediante un parche del StatefulSet antes de su creación. Cambiar una plantilla de PVC de un StatefulSet ya creado tiene restricciones de Kubernetes y no traslada datos automáticamente.

## Monitoreo y despliegue continuo local

El addon Metrics Server proporciona CPU y memoria con `kubectl top nodes` y `kubectl -n cloudcolor top pods`. Las probes startup, liveness y readiness, los endpoints de salud, eventos y logs complementan la observación; no se instaló Prometheus ni Grafana. Para el recorrido:

```powershell
.\scripts\k8s\monitorear-cloudcolor.ps1
.\scripts\k8s\estado-final.ps1
kubectl --context cloudcolor -n cloudcolor logs deployment/cloudcolor-web --tail=30
.\scripts\k8s\probar-rollout-sin-caida.ps1 -Reiniciar
```

El workflow construye y publica las imágenes en GitHub y después ejecuta CD únicamente en un runner local `cloudcolor-k8s` para pushes de `redes/proyecto-final`. El runner debe estar en línea, con acceso al perfil/contexto `cloudcolor`, a kubectl y Minikube. El runner instalado como proceso de usuario deja de funcionar si termina la sesión; no se debe ejecutar código de PR no confiable en él. El despliegue usa `sha-<commit completo>`, verifica dos réplicas, endpoints y health por un port-forward temporal y no vuelve a ejecutar los Jobs de inicialización.

Para volver a la revisión anterior después de revisar compatibilidad y estado:

```powershell
kubectl --context cloudcolor -n cloudcolor rollout history deployment/cloudcolor-web
kubectl --context cloudcolor -n cloudcolor rollout undo deployment/cloudcolor-web
kubectl --context cloudcolor -n cloudcolor rollout status deployment/cloudcolor-web --timeout=180s
```

Una migración de datos necesita planificación y ejecución separadas; un rollback de la web no revierte el esquema. SQL Server utiliza un único Pod: la recreación con el mismo PVC demuestra persistencia, no alta disponibilidad de SQL ni respaldo.
