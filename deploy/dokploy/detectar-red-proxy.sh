#!/usr/bin/env bash
set -euo pipefail
command -v docker >/dev/null || { echo 'Docker no esta disponible.' >&2; exit 1; }
echo 'Redes con nombres relacionados con Dokploy o Traefik:'
docker network ls --format '{{.Name}}' | grep -Ei 'dokploy|traefik' || true
echo 'Redes y subredes de contenedores Traefik activos:'
found=0
while IFS= read -r container; do
  [[ -z "$container" ]] && continue
  found=1
  echo "Contenedor: $container"
  while IFS= read -r network; do
    [[ -z "$network" ]] && continue
    subnet=$(docker network inspect --format '{{range .IPAM.Config}}{{.Subnet}} {{end}}' "$network")
    printf '  Red: %s; subred(es): %s\n' "$network" "$subnet"
  done < <(docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{"\n"}}{{end}}' "$container")
done < <(docker ps --filter name=traefik --format '{{.Names}}')
if (( found == 0 )); then echo 'No se encontro un contenedor Traefik activo; revise la instalacion.'; fi
echo 'Verifique con Preview Compose la red compartida y la IP de origen real antes de elegir DOKPLOY_PROXY_CIDR.'
echo 'No confie en 0.0.0.0/0 ni en redes cuyos miembros no sean confiables; no se cambio ninguna red.'
