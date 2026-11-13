#!/usr/bin/env bash
set -uo pipefail
section() { printf '\n== %s ==\n' "$1"; }
section 'Sistema y arquitectura'
if [[ -r /etc/os-release ]]; then . /etc/os-release; echo "${PRETTY_NAME:-Desconocido}"; fi
uname -m
section 'CPU y memoria'
command -v lscpu >/dev/null && lscpu | grep -E '^(CPU\(s\)|Model name|Architecture):' || true
command -v free >/dev/null && free -h || true
section 'Almacenamiento'
df -h
section 'Docker y Compose'
if command -v docker >/dev/null; then docker --version; docker compose version; docker system df || true; else echo 'Docker no disponible.'; fi
section 'Puertos y servicios en escucha'
if command -v ss >/dev/null; then ss -ltnp; echo 'Puertos requeridos: 80, 443 y 3000.'; else echo 'ss no disponible.'; fi
section 'Fail2ban y firewall'
if command -v systemctl >/dev/null; then systemctl is-active fail2ban || true; fi
if command -v fail2ban-client >/dev/null; then fail2ban-client status sshd || true; fi
if command -v ufw >/dev/null; then ufw status || true; fi
section 'Fecha y zona horaria'
date -Is
command -v timedatectl >/dev/null && timedatectl || true
