# Plan mínimo de evidencias

Tomar las capturas únicamente durante el recorrido final limpio, ocultando credenciales y datos sensibles. Cuando sea legible, agrupar elementos en una misma captura:

1. Docker Compose local: servicios web y SQL activos y aplicación funcionando; mostrar un cambio local con hot reload.
2. Docker Hub y GitHub Actions: etiquetas de las tres imágenes y ejecución de CI/CD del commit mostrado.
3. Estado Kubernetes: nodo, Deployment 2/2, dos Pods, Services, Ingress, PVC y Jobs; usar `estado-final.ps1`.
4. Navegador en `https://cloudcolor.local`: certificado confiable, aplicación y respuesta de salud.
5. Monitoreo: CPU y memoria de nodo y Pods, health y eventos con `monitorear-cloudcolor.ps1`.
6. Alta disponibilidad: dos Pods antes; captura durante la eliminación controlada de uno y otra tras el reemplazo, con respuestas correctas.
7. Hot update final: cambio visible y commit/push; workflow exitoso, imagen SHA, rollout con solicitudes sin fallos observados y el cambio en navegador.

No capturar contenido de `.env`, Secrets Kubernetes, tokens, certificados privados ni cuentas o contraseñas.
