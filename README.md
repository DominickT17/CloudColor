# CloudColor

Proyecto Final de Bases de Datos II para administrar una empresa de pinturas y productos relacionados. La base de datos concentra el catálogo, compras con entregas parciales, inventario por ubicación y lote, cotizaciones, facturas, pagos, devoluciones y auditoría.

## Tecnología

- Microsoft SQL Server 2022 Express y T-SQL.
- SQL Server Management Studio (SSMS) para ejecutar los scripts.
- DrawDB / DBML para el modelo relacional.
- Aplicación de escritorio multicapa en C#/.NET 9 y Windows Forms.
- Microsoft.Data.SqlClient 6.1.6 (ADO.NET), restaurado mediante NuGet.

## Archivos

- `docs/enunciado/Enunciado_Proyecto_Final_2026.pdf`: enunciado oficial.
- `database/model/CloudColor_Modelo_Relacional.dbml`: modelo aprobado, conservado sin cambios.
- `database/scripts/00_CreateDatabase.sql`: creación de CloudColorDB.
- `database/scripts/01_CreateTables.sql`: 31 tablas y restricciones.
- `database/scripts/02_InitialData.sql`: catálogos mínimos.
- `database/tests/01_TestStructure.sql`: verificaciones de estructura e integridad.
- `database/scripts/03_Procedures_Fase1.sql`: 33 procedimientos de Fase I.
- `database/scripts/04_Functions_Fase1.sql`: función de nombre completo del empleado.
- `database/scripts/05_Views_Fase1.sql`: vistas de usuarios/roles y productos.
- `database/tests/02_TestFase1.sql`: pruebas de procedimientos, función y vistas.
- `docs/fase1/Normalizacion_3FN.md`: justificación aplicada al modelo.
- `docs/fase1/Estado_Fase1.md`: inventario y evidencia de Fase I.
- `src/CloudColor.sln`: solución de cuatro capas y proyecto de pruebas.

## Ejecución en SSMS

Conectarse a SQL Server 2022 Express, por ejemplo `.\SQLEXPRESS`, con permisos de creación de base y objetos. Abrir y ejecutar completos, en este orden:

1. `database/scripts/00_CreateDatabase.sql`
2. `database/scripts/01_CreateTables.sql`
3. `database/scripts/02_InitialData.sql`
4. `database/scripts/03_Procedures_Fase1.sql`
5. `database/scripts/04_Functions_Fase1.sql`
6. `database/scripts/05_Views_Fase1.sql`
7. `database/tests/01_TestStructure.sql`
8. `database/tests/02_TestFase1.sql`

Detenerse si un archivo devuelve un error. Los scripts seleccionan explícitamente `CloudColorDB`. El primero conserva una base existente. El segundo requiere una base sin tablas de usuario y rechaza su repetición sin reemplazar objetos; crea toda la estructura en una transacción. El tercero puede repetirse: solo inserta nombres ausentes, sin sobrescribir registros existentes.

Los scripts 03 a 05 usan CREATE PROCEDURE, CREATE FUNCTION y CREATE VIEW: se instalan una vez y no son migraciones repetibles. Si la etapa inicial ya existe, comenzar en 03. En el entorno local actual ya están instalados todos los objetos; no repetir su creación.

Los procedimientos de listado de usuarios y productos utilizan las vistas del script 05; ejecutar toda la secuencia antes de abrir la aplicación. SQL Server permite crear los procedimientos del script 03 antes de que existan esas vistas mediante resolución diferida de nombres. Para actualizar una instalación anterior a la corrección final, ejecutar únicamente los bloques de `sp_ProductoListar` y `sp_UsuarioListar` del script 03, sustituyendo `CREATE PROCEDURE` por `ALTER PROCEDURE` y usando CloudColorDB; las vistas de 05 deben estar ya instaladas. No repetir el archivo 03 completo.

Las pruebas se ejecutan sin transacciones abiertas ni escrituras concurrentes. Utilizan datos sintéticos y los revierten. No dejan clientes, empleados, usuarios ni facturas de prueba. Los valores IDENTITY consumidos no se recuperan con ROLLBACK, por lo que pueden aparecer saltos normales en los identificadores.

## Estructura y reglas

Se implementan las 31 tablas, 43 FK, 31 PK con IDENTITY y las 23 restricciones UNIQUE del DBML. Los nombres físicos de restricciones siguen `PK_Tabla`, `FK_Tabla_Columna`, `UQ_Tabla_Columnas` y `CK_Tabla_Regla`; el DBML no especifica nombres físicos para ellas.

La regla confirmada es `Cotizacion 1 -> 0..1 Factura`. El índice `UX_Factura_CotizacionID_NotNull` impide reutilizar una cotización, pero permite múltiples facturas con `CotizacionID = NULL`. No se añade UNIQUE sin filtro a esa columna. Esta decisión se documenta aquí sin modificar el DBML.

Los 17 CHECK validan cantidades positivas, precios y costos no negativos, stock mínimo no negativo, disponibilidad entre cero y la cantidad inicial, pagos positivos y totales entre cero y el importe original. Los movimientos exigen cantidad distinta de cero; su convenio de signos y tipos se definirá con los procedimientos. No se fijan listas de estados que el enunciado no define. Ninguna FK utiliza eliminación en cascada.

La unicidad compuesta de ProductoVariante se conserva tal como está en el modelo, incluido ColorID opcional: no se permiten variantes repetidas con la misma combinación de producto y presentación sin color.

Los CHECK no sustituyen las futuras validaciones entre tablas: correspondencia entre orden y recepción, variante y lote vendido, factura y devolución; límites acumulados de recepción/devolución; actualización de existencias; PEPS; pagos y totales; reversión de inventario sin duplicar devoluciones previas. Se resolverán mediante operaciones transaccionales en la siguiente etapa.

## Datos iniciales

- Roles: Digitador, Cajero y Gerente.
- Categorías: Accesorios, Solventes, Pinturas, Barnices, Lacas y Selladores.
- Medios de pago: Efectivo, Cheque y Tarjeta.
- Presentaciones: Unidad; 1/32, 1/16, 1/8, 1/4, 1/2 y 1 galón; 1 cubeta.

Son 20 registros. Las presentaciones son catálogos, no conversiones de volumen ni reglas que autoricen cualquier presentación para cualquier producto. No se presume el volumen de una cubeta. No se cargan marcas, colores, bodegas ni datos comerciales ficticios. No hay cuentas ni contraseñas iniciales.

## Solución y requisitos de ejecución

Se requiere Windows, SQL Server Express y .NET 9 Windows Desktop. Para compilar se requiere el SDK .NET 9; se verificó con SDK 9.0.318 y Windows Desktop 9.0.20 ya instalados.

| Proyecto | Responsabilidad |
|---|---|
| CloudColor.Entidades | Entidades y resultados de consulta |
| CloudColor.Datos | Conexión, ADO.NET, parámetros y procedimientos |
| CloudColor.Negocio | Validaciones, permisos y autenticación |
| CloudColor.Presentacion | Formularios WinForms |

Presentación llama a Negocio, Negocio a Datos y Datos a SQL Server. Los formularios no contienen SQL ni comandos SqlClient. No se utiliza Entity Framework. El proyecto adicional tests/CloudColor.Pruebas verifica las capas sin un framework de pruebas externo.

## Configurar la conexión, compilar y ejecutar

La cadena está en `src/CloudColor.Presentacion/conexion.json` y se copia junto al ejecutable. Usa `.\SQLEXPRESS`, CloudColorDB y autenticación integrada de Windows. Puede crearse `conexion.local.json` con la misma estructura para una configuración distinta; queda ignorado por Git y también se copia al compilar. Un archivo local junto al ejecutable tiene prioridad sobre conexion.json.

TrustServerCertificate=True permite el certificado local de Express; para un servidor con certificado confiable se debe usar False. No se guardan contraseñas de SQL Server: se requiere autenticación integrada.

Desde la raíz del repositorio:

```powershell
dotnet restore src/CloudColor.sln
dotnet build src/CloudColor.sln -c Release
dotnet run --project src/CloudColor.Presentacion -c Release
```

También puede ejecutarse `src/CloudColor.Presentacion/bin/Release/net9.0-windows/CloudColor.Presentacion.exe`, conservando los demás archivos de esa carpeta.

## Configuración inicial y autenticación

Si no hay usuarios, se solicita código de empleado, nombres, apellidos, usuario y contraseña elegida por la persona. Se crean empleado y cuenta Gerente en una transacción. El bloqueo de Usuario durante el alta impide dos configuraciones iniciales simultáneas. Después de existir una cuenta se muestra el login. No hay contraseña predeterminada.

Negocio genera PBKDF2-HMAC-SHA256 con 600 000 iteraciones, salt aleatorio de 16 bytes y resultado de 32 bytes. La contraseña admite entre 12 y 128 caracteres. PasswordHash conserva un único string `PBKDF2$SHA256$iteraciones$saltBase64$hashBase64`. La verificación utiliza comparación criptográfica de tiempo constante. La contraseña no se escribe en logs ni se envía a SQL; las vistas y cuadrículas no muestran hashes.

SQL Server nunca recibe ni almacena la contraseña original: únicamente persiste el verificador protegido en PasswordHash. La contraseña escrita circula temporalmente entre el formulario y Negocio en memoria del proceso C#. Al crear una cuenta, `sp_ConfiguracionInicial` o `sp_UsuarioCrear` recibe el verificador generado por Negocio. Al iniciar sesión, `sp_UsuarioObtenerPorNombreUsuario` recibe el nombre de usuario y devuelve el verificador junto con los datos de la cuenta; Negocio realiza la comprobación PBKDF2. Un cambio de contraseña también envía solamente el nuevo verificador. No se implementan HASHBYTES, cifrado simétrico ni otra criptografía de contraseñas en SQL Server.

El costo sigue la [referencia de almacenamiento de contraseñas de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html). Un hash no es cifrado reversible. El enunciado dice “cifrada desde la base de datos”; por decisión técnica confirmada, C# genera y compara el hash, y SQL lo almacena y entrega al login. Debe explicarse esa diferencia en la revisión académica, sin afirmar que SQL realiza el cifrado.

El login verifica usuario, empleado y rol activos. Negocio revalida la sesión antes de cada operación. Los perfiles son permisos de la aplicación, no logins independientes de SQL Server. Los procedimientos verifican al usuario actor, pero una persona con permisos directos de administración de la base puede ejecutar SQL al margen de la aplicación; este proyecto no sustituye la seguridad de la cuenta Windows.

## Módulos y permisos de Fase I

| Perfil | Acceso |
|---|---|
| Digitador | Cliente, Proveedor, Producto y Empleado |
| Gerente | Los cuatro mantenimientos, Usuarios y Roles |
| Cajero | Login y menú; cobros pendientes de otra fase |

Los cuatro mantenimientos permiten listar, buscar, seleccionar, crear, modificar y activar/desactivar. Se conserva el historial mediante Activo. Producto selecciona categoría y marca con ComboBox; Nueva marca permite registrar una marca real en el catálogo inicialmente vacío, sin convertirse en otro módulo principal. PrecioVentaActual pertenece a ProductoVariante: no se agrega un precio a Producto ni se implementa el mantenimiento de variantes en esta etapa.

Al crear una marca desde un producto en edición, se recargan los catálogos conservando la categoría y seleccionando el ID de la marca recién creada; nombre, descripción y opciones del producto mantienen sus valores pendientes. Cancelar el diálogo conserva la selección existente. `sp_ProductoListar` consulta `vw_ProductosDetalle` y `sp_UsuarioListar` consulta `vw_UsuariosRoles`, que utiliza `fn_EmpleadoNombreCompleto`; los resultados conservan las columnas, orden, tipos y nulabilidad consumidos por C#.

Usuarios permite seleccionar empleado y rol, crear cuentas, modificar datos, cambiar rol y activar/desactivar. Al editar, contraseña vacía conserva el hash; una nueva lo reemplaza. Modificar usuario y cambiar rol se ejecutan en una transacción.

Roles permite modificar descripción y estado; los tres nombres se conservan para mantener la matriz de permisos. No se añade Administrador. No se permite desactivar Gerente, modificar la propia cuenta desde administración ni desactivar un empleado con cuenta Gerente activa. Para cambiar esa cuenta se debe usar otra cuenta Gerente y resolver antes el estado de la cuenta asociada.

## Pruebas de aplicación

```powershell
dotnet run --project tests/CloudColor.Pruebas -c Release
```

La suite requiere los scripts instalados y una base sin datos de negocio, antes de crear la primera cuenta real. Rechaza la ejecución si ya hay usuarios y nunca elimina cuentas existentes. Genera credenciales efímeras, acciona formularios fuera del área visible y revierte todos los datos mediante una transacción ambiental. No ejecutar durante uso concurrente. Las capturas quedan en `%TEMP%/CloudColor-Pruebas`, fuera del repositorio.

Incluye la regresión de Nueva marca durante la edición de Producto: cancelar, conservar una categoría distinta de la primera, seleccionar la nueva marca, mantener los demás campos y guardar sobre el mismo ProductoID. La prueba SQL 02 valida los listados que reutilizan las vistas y rechaza un nombre completo NULL además de un valor incorrecto.

Después de crear cuentas reales, preparar un entorno SQL separado con los mismos scripts y configurar la conexión del ejecutable de pruebas. No borrar datos reales para repetir la suite.

## Estado

Objetos SQL instalados y probados, solución compilada y formularios verificados. Las correcciones finales posteriores a la auditoría están documentadas en `docs/fase1/Estado_Fase1.md`. Quedan el alta de la primera cuenta real y la aprobación final antes de integrar feat/fase1 a main.

Facturación, compras, impresión, PEPS operativo, reportes, devoluciones y anulaciones pertenecen a etapas posteriores; no están implementados en esta UI.
