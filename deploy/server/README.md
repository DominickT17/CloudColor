# Seguridad y diagnóstico del servidor

`instalar-fail2ban.sh` está preparado para Ubuntu/Debian con systemd y debe ejecutarse con privilegios administrativos **solo en el VPS previsto**. Instala Fail2ban si falta, configura únicamente `jail.d/cloudcolor-sshd.local`, valida con `fail2ban-client -t` y activa el servicio. Usa `sshd`, cinco intentos en diez minutos y bloqueo de una hora. Si SSH utiliza un puerto distinto del servicio `ssh`, adapte `port` antes de usarlo. Compruebe que tiene acceso administrativo alternativo para evitar un bloqueo accidental. No configura filtros de Traefik ni modifica `jail.conf`.

`verificar-servidor.sh` es de lectura: muestra sistema, CPU, memoria, disco, Docker/Compose, puertos en escucha, estado de Fail2ban/UFW y hora. Algunos datos pueden requerir permisos de lectura adicionales. No se ejecutó ni se instaló Fail2ban en esta fase. Configure firewall de acceso al panel Dokploy y SSH de acuerdo con el entorno; no exponga SQL Server públicamente.
