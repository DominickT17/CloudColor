FROM mcr.microsoft.com/mssql/server:2022-latest
COPY docker/sql/init.sh /init/init.sh
COPY database/scripts/ /scripts/
ENTRYPOINT ["/bin/bash", "/init/init.sh"]
