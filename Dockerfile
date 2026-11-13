FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /workspace
COPY src/CloudColor.Web/CloudColor.Web.csproj src/CloudColor.Web/
COPY src/CloudColor.Negocio/CloudColor.Negocio.csproj src/CloudColor.Negocio/
COPY src/CloudColor.Datos/CloudColor.Datos.csproj src/CloudColor.Datos/
COPY src/CloudColor.Entidades/CloudColor.Entidades.csproj src/CloudColor.Entidades/
RUN dotnet restore src/CloudColor.Web/CloudColor.Web.csproj
COPY src/CloudColor.Web/ src/CloudColor.Web/
COPY src/CloudColor.Negocio/ src/CloudColor.Negocio/
COPY src/CloudColor.Datos/ src/CloudColor.Datos/
COPY src/CloudColor.Entidades/ src/CloudColor.Entidades/

FROM development AS publish
RUN dotnet publish src/CloudColor.Web/CloudColor.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloudColor.Web.dll"]
