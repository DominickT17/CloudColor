# Opción A: Kubernetes

## Arquitectura

```text
Desarrollo: editor → Docker Compose → Web → SQL Server
CI:         git push → GitHub Actions → Docker Build → Docker Hub
CD:         GitHub Actions → runner local → Minikube → Deployment → 2 Pods
Acceso:     navegador → HTTPS → Ingress → Service → Pods web
Datos:      Pods web → Service SQL → StatefulSet SQL → PVC
Monitoreo:  Metrics Server + probes + health + logs + eventos
```

| Requisito | Implementación | Evidencia reproducible |
|---|---|---|
| Desarrollo local | Dockerfile, Compose y `dotnet watch` | Aplicación y SQL con `docker compose ps`; edición local visible |
| Registry | Docker Hub, imágenes web, db-init y user-init | Etiquetas `latest` y `sha-<commit>` en Docker Hub |
| CI | `.github/workflows/docker-ci.yml` | Ejecución GitHub Actions tras un push a la rama del proyecto |
| Clúster | Minikube, contexto `cloudcolor` | `kubectl --context cloudcolor get nodes` |
| Deployment, dos réplicas | `k8s/base/web-deployment.yaml` | `kubectl --context cloudcolor -n cloudcolor get deployment,pods` |
| Service | `k8s/base/web-service.yaml` | `kubectl --context cloudcolor -n cloudcolor get svc,endpointslices` |
| Ingress y HTTPS | Overlay local, `cloudcolor.local`, Secret TLS local de mkcert | Abrir `https://cloudcolor.local/Login`; HTTP redirige a HTTPS |
| Monitoreo | Metrics Server, probes, health, logs y eventos | `./scripts/k8s/monitorear-cloudcolor.ps1` |
| Alta disponibilidad web | Dos réplicas, readiness, Service y PDB | `./scripts/k8s/probar-alta-disponibilidad.ps1 -EliminarUnPod` |
| RollingUpdate | `maxUnavailable: 0`, `maxSurge: 1` | `./scripts/k8s/probar-rollout-sin-caida.ps1 -Reiniciar` |
| Persistencia SQL | StatefulSet y PVC de 5 GiB | `kubectl --context cloudcolor -n cloudcolor get statefulset,pvc` |
| Sesiones compartidas | Data Protection en SQL, mismo nombre de aplicación | Login por HTTPS, cookie aceptada entre réplicas y tras reemplazo |
| CD | Runner self-hosted Windows, imagen `sha-<commit>` | Job Kubernetes después de la publicación y `./scripts/k8s/estado-final.ps1` |
| Cambio visible sin caída | Mismo CI/CD y prueba continua durante rollout | Pendiente: cambio visible, push, workflow, solicitudes y navegador |

La infraestructura local requiere Minikube y el runner en línea. Ambos Pods web comparten un solo nodo; la redundancia no cubre la caída de ese nodo. SQL Server usa una sola instancia: el PVC conserva datos, pero no representa HA ni sustituye respaldos. La CA mkcert es para uso local. El directorio de certificados, el kubeconfig y las credenciales nunca se versionan. Consulte `k8s/README.md` para TLS, actualización, monitoreo y rollback.
