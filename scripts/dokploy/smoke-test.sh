#!/usr/bin/env bash
set -euo pipefail
if [[ $# -ne 1 || ! "$1" =~ ^https?://[^/?#@[:space:]]+(/[^?#[:space:]]*)?$ ]]; then
  echo 'Uso: smoke-test.sh https://dominio (sin credenciales ni parametros)' >&2
  exit 2
fi
base=${1%/}
for path in /health/live /health/ready /Login; do
  success=0
  for ((attempt=1; attempt<=20; attempt++)); do
    code=$(curl --proto '=http,https' --fail --silent --show-error --max-time 10 --output /dev/null --write-out '%{http_code}' "$base$path" 2>/dev/null) || code=000
    if [[ "$code" == 200 ]]; then success=1; break; fi
    if (( attempt < 20 )); then sleep 5; fi
  done
  if (( success == 0 )); then echo "$path no devolvio HTTP 200." >&2; exit 1; fi
  echo "$path: HTTP 200"
done
