# Guía técnica corta de demostración

Ejecutar desde la raíz del clon. No mostrar `.env`, Secrets ni credenciales.

1. Arquitectura: explicar el flujo de `OPCION-A-KUBERNETES.md`.
2. Docker local: `docker compose ps`; abrir la web local y mostrar hot reload.
3. Git: `git status --short --branch` y `git log -1 --oneline`.
4. Docker Hub: mostrar las etiquetas web, db-init y user-init.
5. Kubernetes y dos Pods: `./scripts/k8s/estado-final.ps1`.
6. HTTPS: abrir `https://cloudcolor.local/Login`; verificar el certificado y la redirección HTTP.
7. Monitoreo: `./scripts/k8s/monitorear-cloudcolor.ps1`.
8. HA: iniciar sesión; `./scripts/k8s/probar-alta-disponibilidad.ps1 -EliminarUnPod`; observar una réplica disponible y el reemplazo.
9. Actualización: preparar una edición visible inocua; iniciar monitoreo continuo de disponibilidad, hacer commit y push a `redes/proyecto-final`, observar Docker CI y el job Kubernetes.
10. Confirmar imagen `sha-<commit>`, dos Pods y health con `./scripts/k8s/estado-final.ps1`; mostrar el cambio en el navegador y el resultado del monitoreo.

Durante el push el runner local y Minikube deben estar activos; el túnel debe estar disponible para acceso al Ingress. `probar-rollout-sin-caida.ps1 -Sha <SHA-publicado>` mide un rollout controlado independiente; no dispara ni sustituye la demostración real de push y cambio visible.
