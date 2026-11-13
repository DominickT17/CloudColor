#!/usr/bin/env bash
set -euo pipefail
if (( EUID != 0 )); then echo 'Ejecute con sudo en el VPS Ubuntu/Debian.' >&2; exit 1; fi
if [[ ! -r /etc/os-release ]]; then echo 'No se pudo identificar la distribucion.' >&2; exit 1; fi
. /etc/os-release
if [[ " ${ID:-} ${ID_LIKE:-} " != *" debian "* && " ${ID:-} ${ID_LIKE:-} " != *" ubuntu "* ]]; then
  echo 'Este instalador requiere Ubuntu o Debian.' >&2; exit 1
fi
command -v systemctl >/dev/null && [[ -d /run/systemd/system ]] || { echo 'Se requiere systemd.' >&2; exit 1; }
if ! command -v fail2ban-client >/dev/null; then
  apt-get update
  DEBIAN_FRONTEND=noninteractive apt-get install -y fail2ban
fi
install -d -m 755 /etc/fail2ban/jail.d
target=/etc/fail2ban/jail.d/cloudcolor-sshd.local
candidate=$(mktemp /etc/fail2ban/jail.d/.cloudcolor-candidate.XXXXXX)
backup=$(mktemp /etc/fail2ban/jail.d/.cloudcolor-backup.XXXXXX)
trap 'rm -f -- "$candidate" "$backup"' EXIT
cat > "$candidate" <<'EOF'
[sshd]
enabled = true
backend = systemd
port = ssh
maxretry = 5
findtime = 10m
bantime = 1h
EOF
had_previous=0
if [[ -f "$target" ]]; then cp -p -- "$target" "$backup"; had_previous=1; fi
if ! cmp -s -- "$candidate" "$target"; then install -m 644 -- "$candidate" "$target"; fi
if ! fail2ban-client -t; then
  if (( had_previous )); then cp -p -- "$backup" "$target"; else rm -f -- "$target"; fi
  echo 'Configuracion invalida; se restauro el estado anterior.' >&2
  exit 1
fi
if systemctl is-active --quiet fail2ban; then systemctl restart fail2ban; fi
systemctl enable --now fail2ban
fail2ban-client status
fail2ban-client status sshd
