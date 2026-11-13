#!/usr/bin/env python3
"""Solicita un despliegue y espera su resultado sin exponer credenciales."""
import json
import os
import subprocess
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def api_request(base, key, path, body=None):
    request = Request(base + path, data=None if body is None else json.dumps(body).encode(),
                      headers={"x-api-key": key, "Content-Type": "application/json"},
                      method="GET" if body is None else "POST")
    try:
        with build_opener(NoRedirect()).open(request, timeout=20) as response:
            return json.load(response)
    except HTTPError as exc:
        raise RuntimeError(f"La API Dokploy devolvio HTTP {exc.code}.") from None
    except (URLError, TimeoutError, ValueError, OSError):
        raise RuntimeError("No se pudo consultar la API Dokploy o su respuesta no es JSON valido.") from None


def deployment_list(base, key, compose_id):
    query = urlencode({"composeId": compose_id})
    data = api_request(base, key, "/api/deployment.allByCompose?" + query)
    if not isinstance(data, list) or not all(isinstance(item, dict) for item in data):
        raise RuntimeError("La API Dokploy devolvio un historial de despliegues incompatible.")
    return data


def main():
    base = os.getenv("DOKPLOY_URL", "").strip().rstrip("/")
    compose_id = os.getenv("DOKPLOY_COMPOSE_ID", "").strip()
    key = os.getenv("DOKPLOY_API_KEY", "").strip()
    if not all((base, compose_id, key)):
        print("Despliegue Dokploy pendiente de configuración")
        return 0
    parsed = urlsplit(base)
    if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password or parsed.query or parsed.fragment:
        raise RuntimeError("DOKPLOY_URL debe ser una URL HTTPS sin credenciales ni parametros.")
    if any(item.get("status") == "running" for item in deployment_list(base, key, compose_id)):
        raise RuntimeError("Ya existe un despliegue Dokploy en curso; no se iniciara otro.")

    run_id = os.getenv("GITHUB_RUN_ID", "")
    attempt = os.getenv("GITHUB_RUN_ATTEMPT", "")
    sha = os.getenv("GITHUB_SHA", "")
    if not run_id.isdigit() or not attempt.isdigit() or len(sha) != 40 or any(c not in "0123456789abcdef" for c in sha):
        raise RuntimeError("Falta la identidad valida de la ejecucion GitHub.")
    title = f"CloudColor Actions {run_id}-{attempt}"
    payload = {"composeId": compose_id, "title": title,
               "description": f"CloudColor commit {sha}", "freshVolumes": False}
    api_request(base, key, "/api/compose.redeploy", payload)
    print("Redeploy solicitado; esperando el resultado de Dokploy.", flush=True)

    deadline = time.monotonic() + 900
    while time.monotonic() < deadline:
        entries = [entry for entry in deployment_list(base, key, compose_id) if entry.get("title") == title]
        if len(entries) > 1:
            raise RuntimeError("Hay varias ejecuciones Dokploy con la misma identidad; revise manualmente.")
        if entries:
            state = entries[0].get("status")
            if state == "done":
                print("Dokploy informo que el despliegue termino.", flush=True)
                break
            if state in ("error", "cancelled"):
                raise RuntimeError("Dokploy informo que el despliegue fallo o fue cancelado.")
            if state != "running":
                raise RuntimeError("Dokploy devolvio un estado de despliegue desconocido.")
        time.sleep(5)
    else:
        raise RuntimeError("Tiempo agotado esperando Dokploy; el despliegue puede seguir activo. Reviselo antes de reintentar.")

    public_url = os.getenv("CLOUDCOLOR_PUBLIC_URL", "").strip()
    if not public_url:
        print("Smoke test omitido: CLOUDCOLOR_PUBLIC_URL no configurada.")
        return 0
    smoke_env = {name: value for name, value in os.environ.items() if name != "DOKPLOY_API_KEY"}
    return subprocess.run(["bash", "scripts/dokploy/smoke-test.sh", public_url],
                          check=False, env=smoke_env).returncode


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, OSError) as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
