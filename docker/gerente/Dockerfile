FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace
COPY src/CloudColor.Inicializador/CloudColor.Inicializador.csproj src/CloudColor.Inicializador/
COPY src/CloudColor.Negocio/CloudColor.Negocio.csproj src/CloudColor.Negocio/
COPY src/CloudColor.Datos/CloudColor.Datos.csproj src/CloudColor.Datos/
COPY src/CloudColor.Entidades/CloudColor.Entidades.csproj src/CloudColor.Entidades/
RUN dotnet restore src/CloudColor.Inicializador/CloudColor.Inicializador.csproj
COPY src/CloudColor.Inicializador/ src/CloudColor.Inicializador/
COPY src/CloudColor.Negocio/ src/CloudColor.Negocio/
COPY src/CloudColor.Datos/ src/CloudColor.Datos/
COPY src/CloudColor.Entidades/ src/CloudColor.Entidades/
RUN dotnet publish src/CloudColor.Inicializador/CloudColor.Inicializador.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish/ ./
USER $APP_UID
ENTRYPOINT ["dotnet", "CloudColor.Inicializador.dll"]
