# Demostración de hot update

1. Confirme que la aplicación y SQL están sanos y que el volumen tiene respaldo.
2. Haga un cambio visible y pequeño en la interfaz de una rama autorizada y revíselo.
3. Después de aprobación, realice commit y push a `redes/proyecto-final`.
4. Observe en GitHub Actions la construcción y publicación de las tres imágenes en Docker Hub.
5. Con Dokploy configurado, Actions solicita el redeploy y espera su resultado; el Auto Deploy nativo de Dokploy debe estar desactivado.
6. Verifique en logs de Dokploy que se descargaron las imágenes esperadas y que los inicializadores terminaron. El flujo con tags fijos SHA requiere actualizarlos explícitamente.
7. Abra la web y confirme el texto nuevo; compruebe `/health/live`, `/health/ready` y `/Login`.
8. Confirme que el volumen, la base, el esquema y los usuarios persisten siguiendo [PERSISTENCIA.md](PERSISTENCIA.md).

La API de Dokploy encola el trabajo; HTTP 200 de la API y health 200 no prueban por sí solos que la imagen nueva esté sirviendo. Contraste tag/digest y el cambio visible. Compose con una sola web puede tener una interrupción durante el reemplazo; esta demostración no garantiza cero downtime.

La operación `compose.redeploy` revisada en el código actual de Dokploy usa el checkout existente. Si el cambio incluye `compose.production.yaml`, primero actualice la fuente mediante un Deploy controlado; el redeploy automático de imágenes no garantiza obtener ese cambio.
