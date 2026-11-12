# CloudColor

Proyecto académico integrado de Bases de Datos II, Programación Web e Ingeniería de Software II para administrar una empresa de pinturas y productos relacionados. El alcance implementado cubre Fase I, Fase II y el proyecto final: catálogos, usuarios y roles, compras con entregas parciales, inventario por ubicación y lote, cotizaciones, facturación, cobros, devoluciones, anulaciones, reportes y auditoría. El modelo aprobado conserva exactamente 31 tablas. La rama de trabajo es `feat/proyecto-final`; el estado de la revisión se registra en el checklist.

## Tecnología

- Microsoft SQL Server 2022 Express y T-SQL.
- SQL Server Management Studio (SSMS) para ejecutar los scripts.
- DrawDB / DBML para el modelo relacional.
- Aplicación de escritorio multicapa en C#/.NET 9 y Windows Forms.
- ASP.NET Core MVC/Razor, HTML, CSS y JavaScript para la presentación web.
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
- `database/scripts/06_Types_Fase2.sql`: cuatro tipos tabla para parámetros TVP.
- `database/scripts/07_Views_Fase2.sql`: variantes e inventario por ingreso.
- `database/scripts/08_Procedures_Catalogos_Fase2.sql`: autorización, variantes, bodegas, ubicaciones y proveedores por variante.
- `database/scripts/09_Procedures_Cotizacion.sql`: cotizaciones y confirmación.
- `database/scripts/10_Procedures_Facturacion.sql`: emisión con PEPS, cobro y consultas.
- `database/scripts/11_Procedures_Compras.sql`: órdenes y recepciones parciales/completas.
- `database/scripts/12_Datos_Demostracion_Fase2.sql`: carga opcional y reproducible de la demostración comercial.
- `database/scripts/13_Types_ProyectoFinal.sql`: TVP para detalles de devoluciones.
- `database/scripts/14_Views_ProyectoFinal.sql`: ventas netas históricas por detalle.
- `database/scripts/15_Procedures_ProyectoFinal.sql`: reportes, consulta de lotes, devolución y anulación transaccionales.
- `database/scripts/16_Sincronizar_Procedimientos.sql`: actualiza seis procedimientos existentes para alinear pagos y auditoría con el código actual.
- `database/tests/03_TestFase2.sql`: pruebas SQL de Fase II con ROLLBACK.
- `database/tests/04_TestProyectoFinal.sql`: pruebas SQL de reportes, devolución, anulación y permisos.
- `docs/fase2/Estado_Fase2.md`: objetos, flujos, permisos, evidencia y guía de revisión.
- `docs/final/Estado_Proyecto_Final.md`: estado y validación del proyecto final.
- `docs/final/Guia_Presentacion.md`: recorrido sugerido para la exposición.
- `docs/final/Matriz_Trazabilidad.md`: relación de requisitos, capas, SQL y pruebas.
- `docs/final/Checklist_Entrega_Final.md`: alcance probado y pendientes de entrega.

## Ejecución en SSMS

Para una instalación nueva, conectarse a SQL Server 2022 Express, por ejemplo `.\SQLEXPRESS`, con permisos de creación de base y objetos. Abrir y ejecutar completos, en este orden:

1. `database/scripts/00_CreateDatabase.sql`
2. `database/scripts/01_CreateTables.sql`
3. `database/scripts/02_InitialData.sql`
4. `database/scripts/03_Procedures_Fase1.sql`
5. `database/scripts/04_Functions_Fase1.sql`
6. `database/scripts/05_Views_Fase1.sql`
7. `database/scripts/06_Types_Fase2.sql`
8. `database/scripts/07_Views_Fase2.sql`
9. `database/scripts/08_Procedures_Catalogos_Fase2.sql`
10. `database/scripts/09_Procedures_Cotizacion.sql`
11. `database/scripts/10_Procedures_Facturacion.sql`
12. `database/scripts/11_Procedures_Compras.sql`
13. `database/scripts/12_Datos_Demostracion_Fase2.sql` (opcional, una vez configurado el Gerente)
14. `database/scripts/13_Types_ProyectoFinal.sql`
15. `database/scripts/14_Views_ProyectoFinal.sql`
16. `database/scripts/15_Procedures_ProyectoFinal.sql`
17. `database/scripts/16_Sincronizar_Procedimientos.sql`

El script 12 es una carga opcional y reproducible para la demostración comercial; requiere que el Gerente ya esté configurado y se ejecuta después del 11. Si se omite, la base queda sin datos comerciales. Los scripts 13–15 instalan los objetos del proyecto final. El 16 actualiza procedimientos que deben existir previamente; puede repetirse. Las pruebas SQL 01–04 se ejecutan en una base de pruebas. La suite C# prepara automáticamente una base temporal, instala 00–11 y 13–16, omite la carga opcional 12 y corre las cuatro suites sin escribir en CloudColorDB.

En una instalación existente con los scripts 03 y 10 instalados, ejecutar `16_Sincronizar_Procedimientos.sql` completo actualiza `sp_FacturaPagos` y los procedimientos de auditoría de usuarios y roles mediante `ALTER`. No ejecutar el script 12 para esta actualización. Una versión antigua de `sp_FacturaPagos` no devuelve `Cajero` y provoca errores al consultar facturas con pagos.

Detenerse si un archivo devuelve un error. Los scripts seleccionan explícitamente `CloudColorDB`. El primero conserva una base existente. El segundo requiere una base sin tablas de usuario y rechaza su repetición sin reemplazar objetos; crea toda la estructura en una transacción. El tercero puede repetirse: solo inserta nombres ausentes, sin sobrescribir registros existentes.

Los scripts 03 a 05 usan CREATE PROCEDURE, CREATE FUNCTION y CREATE VIEW: se instalan una vez y no son migraciones repetibles. Si la etapa inicial ya existe, comenzar en 03. En el entorno local actual ya están instalados todos los objetos; no repetir su creación.

Para ampliar una instalación de Fase I que ya tenga 00–05, instalar **06 → 07 → 08 → 09 → 10 → 11 → 13 → 14 → 15 → 16** una sola vez. El script 12 es únicamente una carga de demostración y se ejecuta después de preparar una cuenta Gerente. Los scripts 06–11 crean objetos de fase II; 13–15 son repetibles y el 16 actualiza procedimientos existentes. En una base existente, inspeccionar los objetos y ejecutar solamente los que falten; no reconstruir CloudColorDB. Los tipos tabla no se reemplazan automáticamente.

Los procedimientos de listado de usuarios y productos utilizan las vistas del script 05; ejecutar toda la secuencia antes de abrir la aplicación. SQL Server permite crear los procedimientos del script 03 antes de que existan esas vistas mediante resolución diferida de nombres. Para actualizar una instalación anterior a la corrección final, ejecutar únicamente los bloques de `sp_ProductoListar` y `sp_UsuarioListar` del script 03, sustituyendo `CREATE PROCEDURE` por `ALTER PROCEDURE` y usando CloudColorDB; las vistas de 05 deben estar ya instaladas. No repetir el archivo 03 completo.

Las pruebas se ejecutan sin transacciones abiertas ni escrituras concurrentes. Utilizan datos sintéticos y los revierten. No dejan clientes, empleados, usuarios ni facturas de prueba. Los valores IDENTITY consumidos no se recuperan con ROLLBACK, por lo que pueden aparecer saltos normales en los identificadores.

## Estructura y reglas

Se implementan las 31 tablas, 43 FK, 31 PK con IDENTITY y las 23 restricciones UNIQUE del DBML. Los nombres físicos de restricciones siguen `PK_Tabla`, `FK_Tabla_Columna`, `UQ_Tabla_Columnas` y `CK_Tabla_Regla`; el DBML no especifica nombres físicos para ellas.

La regla confirmada es `Cotizacion 1 -> 0..1 Factura`. El índice `UX_Factura_CotizacionID_NotNull` impide reutilizar una cotización, pero permite múltiples facturas con `CotizacionID = NULL`. No se añade UNIQUE sin filtro a esa columna. Esta decisión se documenta aquí sin modificar el DBML.

Los 17 CHECK validan cantidades positivas, precios y costos no negativos, stock mínimo no negativo, disponibilidad entre cero y la cantidad inicial, pagos positivos y totales entre cero y el importe original. Los movimientos exigen cantidad distinta de cero; en Fase II ENTRADA_COMPRA es positiva y SALIDA_VENTA negativa. No se fijan listas de estados que el enunciado no define. Ninguna FK utiliza eliminación en cascada.

La unicidad compuesta de ProductoVariante se conserva tal como está en el modelo, incluido ColorID opcional: no se permiten variantes repetidas con la misma combinación de producto y presentación sin color.

Los CHECK se complementan con procedimientos transaccionales para correspondencia orden/recepción y variante/lote, límite acumulado recibido, existencias suficientes, PEPS, pagos exactos, devoluciones acumuladas y anulación de facturas.

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
| Cajero | Login y menú; consulta de facturas y cobro incorporados en Fase II |

Los cuatro mantenimientos permiten listar, buscar, seleccionar, crear, modificar y activar/desactivar. Se conserva el historial mediante Activo. Producto selecciona categoría y marca con ComboBox; Nueva marca permite registrar una marca real en el catálogo inicialmente vacío, sin convertirse en otro módulo principal. PrecioVentaActual pertenece a ProductoVariante: no se agrega un precio a Producto y su mantenimiento separado se incorpora en Fase II.

Al crear una marca desde un producto en edición, se recargan los catálogos conservando la categoría y seleccionando el ID de la marca recién creada; nombre, descripción y opciones del producto mantienen sus valores pendientes. Cancelar el diálogo conserva la selección existente. `sp_ProductoListar` consulta `vw_ProductosDetalle` y `sp_UsuarioListar` consulta `vw_UsuariosRoles`, que utiliza `fn_EmpleadoNombreCompleto`; los resultados conservan las columnas, orden, tipos y nulabilidad consumidos por C#.

Usuarios permite seleccionar empleado y rol, crear cuentas, modificar datos, cambiar rol y activar/desactivar. Al editar, contraseña vacía conserva el hash; una nueva lo reemplaza. Modificar usuario y cambiar rol se ejecutan en una transacción.

Roles permite modificar descripción y estado; los tres nombres se conservan para mantener la matriz de permisos. No se añade Administrador. No se permite desactivar Gerente, modificar la propia cuenta desde administración ni desactivar un empleado con cuenta Gerente activa. Para cambiar esa cuenta se debe usar otra cuenta Gerente y resolver antes el estado de la cuenta asociada.

## Pruebas de aplicación

```powershell
dotnet run --project tests/CloudColor.Pruebas -c Release
```

La suite crea una base `CloudColor_QA_<GUID>` en la instancia configurada, instala 00–11 y 13–16 y ejecuta las pruebas SQL 01–04. Luego ejecuta Fase I, Fase II y operaciones finales con credenciales efímeras y transacciones que se revierten. CloudColorDB se consulta únicamente para comparar cantidades y huellas del contenido antes/después, sin mostrar contraseñas ni hashes. La base temporal se elimina al terminar, incluso si una prueba lanza un error controlado. No modificar datos de CloudColorDB durante la comparación.

Antes de ejecutar suites C# o web, define localmente la variable `CLOUDCOLOR_QA_PASSWORD` con una clave de prueba temporal. El runner la utiliza solo en la base desechable; no la guardes en archivos ni la compartas. La suite del navegador requiere la misma variable en su proceso.

Se requiere permiso local CREATE/DROP DATABASE; la opción de concurrencia también necesita visibilidad de las sesiones SQL Server. Si faltan permisos, resolverlos en el entorno de pruebas; nunca vaciar CloudColorDB. Una interrupción forzada del proceso puede impedir el finally: en ese caso identificar la base QA exacta antes de una limpieza manual, sin usar eliminaciones por comodines.

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project tests/CloudColor.Pruebas -c Release --no-build -- --concurrencia
```

La opción `--concurrencia` prueba venta, cotización, cobro y recepción mediante dos conexiones y comprueba que la segunda espera el bloqueo antes de rechazar el conflicto. Las capturas de formularios quedan en `%TEMP%/CloudColor-Pruebas`, fuera del repositorio. Para la prueba PrintDocument se necesita una impresora Windows válida (puede ser una impresora virtual ya instalada); se utiliza PreviewPrintController y no se envían documentos a una impresora física.

Incluye la regresión de Nueva marca durante la edición de Producto: cancelar, conservar una categoría distinta de la primera, seleccionar la nueva marca, mantener los demás campos y guardar sobre el mismo ProductoID. La prueba SQL 02 valida los listados que reutilizan las vistas y rechaza un nombre completo NULL además de un valor incorrecto.

## Uso de Fase II

La [guía de entrega web de Fase II](docs/fase2/Entrega_Web_Fase2.md) relaciona cada requisito con su pantalla y SP, explica la elección de UDT/TVP y describe la demostración de cotización, facturación directa, cobros, impresión y recepción parcial/completa.

1. Como Digitador o Gerente, preparar Producto en el mantenimiento de Fase I y luego su ProductoVariante con presentación, color opcional, SKU, precio y stock mínimo. Bodegas, ubicaciones y asociaciones proveedor/variante se administran desde sus pantallas.
2. En Compras y recepciones, seleccionar proveedor y variantes asociadas, cantidades y costos acordados. Guardar la orden y abrir Recibir compra desde Consultar. Cada recepción conserva cantidad, costo real, ubicación, lote proveedor cuando corresponda y fecha de ingreso.
3. Consultar Inventario para ver los ingresos disponibles por lote. Una orden de 100 puede recibir 75 (PARCIAL) y 25 (COMPLETA); 75 + 30 se rechaza.
4. Crear una cotización, consultar y confirmar. Facturar cotización usa sus precios históricos; Facturas también permite emisión directa. La emisión descuenta existencias dentro de una transacción y deja la factura PENDIENTE_PAGO.
5. Como Cajero o Gerente, abrir Facturas / cobros, seleccionar la factura y distribuir el total entre Efectivo, Cheque y Tarjeta. Cheque y Tarjeta requieren referencia. El cobro completo marca PAGADA y no se permite cobrar otra vez.
6. Desde la consulta de factura, usar Vista previa / imprimir. El documento incluye cliente, emisor, detalle, total y medios registrados.

Los TVP envían los detalles como tablas parametrizadas. No se usan JSON/XML en los procedimientos. PEPS utiliza WHILE, FechaIngreso y LoteInventarioID como desempate cuando Producto.AplicaPEPS está activo. Incluso sin número de lote del proveedor existe un lote interno por ingreso. El detalle de lotes y los movimientos conservan cada salida.

Gerente conserva administración de Fase I y puede consultar y operar Fase II. Digitador mantiene catálogos, compras, recepciones, cotizaciones y emisión; no cobra. Cajero consulta/imprime facturas y cobra; no mantiene catálogos ni emite. UI, Negocio y procedimientos verifican esos permisos.

## Proyecto Final: reportes, devoluciones y anulaciones

- Gerente consulta ventas por fecha, medios de pago, cantidad de facturas, productos más/menos vendidos, mayores ingresos y variantes sin stock o bajo mínimo. Los ingresos usan el precio histórico de `DetalleFactura` y las devoluciones reducen unidades e ingresos netos.
- La consulta de factura presenta cliente/NIT, empleado y usuario emisor, líneas, pagos/cajero, total original/actual y, para Gerente, distribución por lote.
- Anular conserva factura y detalle, exige motivo, registra `AnulacionFactura` y `Bitacora`, fija `TotalActual=0` y devuelve al lote original solo las unidades que aún no habían sido devueltas.
- Registrar devolución permite selección por líneas/lotes originales, rechaza cantidades mayores al saldo acumulado, repone el mismo lote y registra `DetalleDevolucion`, movimiento y `Bitacora`. Los dos flujos son transaccionales; no se borra historial.
- Gerente es el único perfil que consulta estos reportes, registra devoluciones y anula. La verificación existe en UI, Negocio y SQL; Digitador y Cajero no obtienen permisos por invocar las rutas directamente.
- Los scripts 13–15 y la suite `database/tests/04_TestProyectoFinal.sql` agregan estos objetos y pruebas sin cambiar las 31 tablas. Ver [estado final](docs/final/Estado_Proyecto_Final.md) y [guía de presentación](docs/final/Guia_Presentacion.md).

## Estado

Fase I, Fase II y Proyecto Final están implementados en esta rama. El documento `docs/fase1/Estado_Fase1.md` y `docs/fase2/Estado_Fase2.md` son snapshots históricos; el estado conjunto está en [Estado_Proyecto_Final.md](docs/final/Estado_Proyecto_Final.md).

Los resultados de cada revisión y sus límites se registran en [Estado_Proyecto_Final.md](docs/final/Estado_Proyecto_Final.md) y [Checklist_Entrega_Final.md](docs/final/Checklist_Entrega_Final.md). La rama `feat/proyecto-final` permanece separada de `main`.

## Interfaz web

Para la preparación local con Docker Compose del Proyecto Final de Redes II, consulte [docker/README.md](docker/README.md). Esta configuración usa una base y credenciales propias del entorno Docker.

`src/CloudColor.Web` es la presentación ASP.NET Core 9 MVC/Razor que reutiliza Negocio, Entidades, Datos y los objetos SQL de Fase I, Fase II y el Proyecto Final. WinForms permanece en la solución; el modelo de 31 tablas no cambia.

```powershell
cd C:\Users\domin\Documents\CloudColor
dotnet run --project .\src\CloudColor.Web
```

Abrir **https://localhost:7255** (certificado de desarrollo confiable requerido) o **http://localhost:5255** para desarrollo local. Usar una cuenta existente de CloudColorDB. Si no existe ninguna, completar primero la configuración inicial en WinForms. No se requiere reinstalar la base para abrir la web.

Incluye Dashboard, catálogos, inventario, compras, recepciones, cotizaciones, facturación, impresión, cobros, reportes, consulta completa de factura, devoluciones, anulaciones y administración según Digitador/Cajero/Gerente. Cookie protegida de 30 minutos, revalidación de permisos por solicitud y antiforgery en POST. La contraseña original nunca se envía a SQL Server.

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project tests/CloudColor.Web.Pruebas -c Release --no-build
```

La suite web utiliza una base QA desechable y el puerto 5256; comprueba que los datos originales se conserven. Ejecución, permisos, rutas, pruebas de navegador y capturas se describen en [Estado_Web.md](docs/web/Estado_Web.md) y en el informe final. No se realizó merge a `main`.

La interfaz incorpora búsqueda global por perfil al escribir en la cabecera, notificaciones, confirmaciones, protección de cambios sin guardar, vistas rápidas y filtros con chips. Cotizaciones, facturas directas y compras permiten editar el detalle sin recargar; Cobros calcula la distribución al instante. Dashboard muestra estados reales, Inventario distingue variantes sin ingresos y las tablas se convierten en tarjetas en móvil. Con QA `--visual` abierto, `npm run test:ux --prefix tests/CloudColor.Web.Browser` valida estos recorridos y genera capturas en cinco tamaños.
