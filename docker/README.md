# CloudColor con Docker Compose

Esta configuración es para desarrollo local. Requiere Docker con contenedores Linux, Compose y recursos suficientes para SQL Server 2022. La imagen web usa .NET 9; esta versión ya terminó su soporte y debe actualizarse en una fase posterior para un entorno de producción.

## Configuración

Desde la raíz de este clon, copie `.env.example` a `.env` y complete sus valores. `.env` está ignorado por Git. Elija contraseñas distintas para SQL Server y las tres cuentas; no comparta el archivo. SQL Server exige una contraseña que cumpla su política. Las contraseñas de las cuentas nuevas deben tener de 12 a 128 caracteres.

Para esta versión, la contraseña de SQL Server admite únicamente letras ASCII, dígitos y los símbolos `_!#%+@-`: se inserta en la cadena de conexión de desarrollo. Si la contraseña del Gerente contiene signos como `$`, utilice comillas simples en `.env` para que Compose la interprete literalmente. `docker compose config` muestra las variables interpoladas, incluidas las contraseñas; para validar sin mostrarlas utilice `docker compose config --quiet`.

La web y el inicializador usan `ConnectionStrings__CloudColor` con `Server=sqlserver,1433`. Para mantener esta fase mínima se usa `sa` también desde la web y se acepta el certificado de desarrollo de SQL Server; son decisiones limitadas al entorno local, no una configuración de producción. Las variables de entorno son visibles para quien tenga acceso a los contenedores. SQL Server no publica su puerto al host; la web se publica únicamente en `127.0.0.1` y, por defecto, queda en `http://localhost:8080`.

## Arranque posterior

Una vez configurado `.env`, en una fase posterior podrá iniciarse con:

```powershell
docker compose up --build
```

Para montar los cuatro proyectos web y usar `dotnet watch`:

```powershell
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

El modo watch usa sondeo de archivos y volúmenes para `bin` y `obj`. Algunos cambios reinician el proceso. La recarga del navegador y la política CSP de la aplicación deben comprobarse durante la prueba práctica.

## Inicialización y persistencia

El servicio `sql-init` espera a que SQL Server acepte conexiones y ejecuta los scripts `00–11` y `13–16`, en ese orden y con `sqlcmd -b`, solo para una base nueva. El script `12` no se ejecuta. Una vez completada la instalación inicial se registra la versión `1`; después el script `17` migra de forma transaccional a `CloudColorDockerSchemaVersion=2`. Las bases existentes con versión `1` reciben solo esa migración; las de versión `2` se verifican sin recrear tablas. Si la marca falta o es desconocida, el proceso se detiene. La marca no comprueba que no se hayan alterado otros objetos.

Una vez instalado el esquema, `gerente-init` usa `SeguridadServicio.Configurar` para crear el primer Gerente si todavía no hay usuarios. Después inicia sesión con el Gerente configurado y utiliza los servicios normales de empleados y usuarios para preparar Digitador y Cajero, con sus roles correspondientes. Todas las altas de esta ejecución se hacen en una transacción. Los nombres de usuario y códigos de empleado deben ser distintos.

Una cuenta existente con el rol, empleado y estado esperados se conserva sin modificar su contraseña. Si el Gerente configurado no puede autenticarse, hay un rol o estado incompatible, o un código de empleado ya está asignado a otra cuenta, el proceso se detiene para revisión. Las variables de las tres cuentas se requieren en Compose incluso si ya existen; cambiarlas después no actualiza las cuentas ni sus contraseñas. El inicializador no comprueba las contraseñas de cuentas Digitador y Cajero existentes.

El volumen nombrado `sqlserver_data` conserva la base y la contraseña de SQL Server entre recreaciones. Cambiar `MSSQL_SA_PASSWORD` en `.env` no cambia por sí solo la contraseña almacenada en un volumen existente. No elimine el volumen para resolver errores sin antes evaluar sus datos; la persistencia no reemplaza las copias de seguridad.

El orden de Compose impide que la web arranque si falla la instalación o el inicializador de usuarios. Compose local mantiene la configuración predeterminada de claves de sesión; Kubernetes activa explícitamente el repositorio SQL compartido descrito en [la guía de Kubernetes](../k8s/README.md). Las pruebas de conectividad, inicialización y persistencia deben hacerse al iniciar los contenedores en una fase posterior.
