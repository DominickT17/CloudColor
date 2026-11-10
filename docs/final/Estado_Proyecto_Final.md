# Estado final de CloudColor

## Revisión del 9 de noviembre de 2026

El detalle de lo probado está en [Checklist_Entrega_Final.md](Checklist_Entrega_Final.md). El Release se restauró y compiló con 0 errores y 0 advertencias. Las pruebas SQL 01–04 y C#/concurrencia se ejecutaron en una base temporal: 130 comprobaciones, con venta, doble facturación de cotización, doble cobro, recepción excesiva, devolución excesiva y doble anulación concurrentes. La base original se mantuvo en 31 tablas y 119 filas, con huellas de contenido iguales antes y después. Al comparar el esquema con una base creada desde los scripts coincidieron 182 columnas, 31 claves primarias y 43 referencias de claves foráneas en los atributos examinados.

La auditoría de referencias encontró en CloudColorDB los 70 procedimientos y cinco TVP nombrados por Datos. Se compararon las 78 definiciones de procedimientos, vistas y función instaladas con la última definición de los scripts 03–16: no hubo diferencias tras normalizar espacios, comentarios de línea y `CREATE OR ALTER`. También se cotejaron los nombres y el orden de las columnas de los cinco TVP. Una comparación estática de 163 parámetros tipados y de los arreglos de los cuatro catálogos principales no encontró diferencias en los nombres y tipos examinados; no sustituye la ejecución de todos los caminos. Los mensajes de negocio de los procedimientos finales se propagan sin perder la excepción SQL interna. La consulta de factura conserva el contexto del menú Reportes al buscar.

La revisión estática adicional de parámetros requeridos examinó 62 llamadas directas; dos posibles omisiones se resolvieron al inspeccionar el método común de marcas y categorías, que aporta `@UsuarioActorID` a ambas. Esta revisión es una comprobación textual y no reemplaza las pruebas de integración.

La regresión HTTP posterior a las correcciones registró 167 comprobaciones correctas. Edge recorrió cotización, facturación, cobro, compra, recepción, catálogos y permisos; las suites también revisaron interactividad y 85 vistas de 17 rutas a cinco tamaños. Se comprobaron búsquedas de las siete entidades por perfil, filtros de inventario, fechas y límites de reportes.

En Edge se accionaron los 23 enlaces del menú de Gerente y se revisó el desbordamiento a cinco tamaños. Se registró una devolución parcial de una factura que había consumido dos lotes y se anuló después, conservando el total histórico. Se corrigió el formulario para que las cantidades de lotes no seleccionados puedan permanecer en cero, mientras el servidor exige cantidades positivas para las líneas seleccionadas. Las 18 capturas seleccionadas en `evidencias/2026-11-09/` muestran datos ficticios de QA y no sustituyen las pruebas SQL de PEPS ni las de concurrencia.

Fecha de corte: 25 de octubre de 2026. Rama de entrega: `feat/proyecto-final`. Este documento registra el estado que se probó en QA aislado; no implica aprobación académica ni merge.

## Revisión en curso del 4 de noviembre de 2026

En `.\SQLEXPRESS/CloudColorDB` se detectó que `sp_FacturaPagos` no devolvía la columna `Cajero` requerida por Datos. Se actualizaron ese procedimiento y cinco procedimientos de usuarios/roles cuya auditoría estaba desactualizada. Se agregó `database/scripts/16_Sincronizar_Procedimientos.sql` para aplicar las mismas definiciones sobre una instalación que ya tenga los scripts 03 y 10. Una factura pagada real volvió a mostrar su detalle, cajero y lotes. La revisión de entrega sigue en curso; consultar [Checklist_Entrega_Final.md](Checklist_Entrega_Final.md) para distinguir las comprobaciones realizadas de las pendientes. Las cifras y capturas siguientes corresponden al corte anterior cuando no se indique otra fecha.

## Alcance y arquitectura

CloudColor administra una empresa de pinturas y productos relacionados. La base SQL Server 2022 Express conserva las 31 tablas del modelo relacional aprobado, sin cambios en DBML, nombres, columnas, PK, FK ni cardinalidades. Los módulos cubren identidad y seguridad, catálogos, compras, recepciones, inventario por lote, cotizaciones, facturación, cobros, impresión, devoluciones, anulaciones, reportes y bitácora.

La solución contiene cuatro capas .NET 9:

```text
CloudColor.Web / CloudColor.Presentacion
              ↓
       CloudColor.Negocio
              ↓
         CloudColor.Datos
              ↓
          SQL Server
```

Web es ASP.NET Core MVC/Razor; Presentacion mantiene WinForms como alternativa. Datos utiliza ADO.NET y `Microsoft.Data.SqlClient`. No se usa Entity Framework ni un backend alternativo. Los formularios y las vistas Razor no emiten SQL: la consulta pasa por Negocio y Datos. Las operaciones reciben parámetros `SqlParameter`/TVP.

## Fase I

Incluye login y configuración inicial del primer Gerente, PBKDF2-HMAC-SHA256 (600 000 iteraciones, salt aleatorio y comparación de tiempo constante), administración de usuarios y roles y los mantenimientos Cliente, Proveedor, Producto y Empleado. Los roles de aplicación son Digitador, Cajero y Gerente. No se agregan identidades SQL para esos perfiles.

La contraseña original solo está temporalmente en memoria de C# durante la entrada y la verificación. SQL Server recibe y almacena `PasswordHash`, el verificador PBKDF2; no recibe la contraseña original. La autorización de cada flujo se comprueba en interfaz, Negocio y procedimientos pertinentes.

## Fase II

Incluye ProductoVariante, bodegas, ubicaciones, asociación proveedor-variante, órdenes de compra, recepción parcial/completa, lotes de inventario, cotización, confirmación, facturación directa o desde cotización, asignación PEPS, pagos combinados e impresión. Una cotización genera como máximo una factura; las facturas directas conservan `CotizacionID = NULL`.

Los detalles de operaciones por lote se pasan con TVP en vez de concatenar JSON/XML. PEPS selecciona los lotes por fecha de ingreso y usa el identificador del lote como desempate. Facturación, cobro y recepción son transaccionales y validan estado/cantidad antes de confirmar.

## Proyecto final

### Reportes y consulta

- Ventas entre fechas muestra total facturado neto, efectivo, cheque, tarjeta y cantidad de facturas; excluye anuladas y aplica las devoluciones al ingreso neto por detalle.
- Más vendidos, mayores ingresos y menos vendidos usan el precio histórico de `DetalleFactura`. Menos vendidos incluye variantes activas con cero ventas. Los reportes de producto aceptan Top 5, Top 10 y todos desde la interfaz.
- El reporte de inventario/sin stock muestra disponible, mínimo, estado y proveedor asociado cuando existe.
- Consulta de factura presenta cliente/NIT, emisor, usuario, partidas, pagos/cajero, importes original/actual y lotes al Gerente. Conserva acceso de impresión.

### Devolución y anulación

La devolución se limita por la cantidad vendida y devuelta previamente en cada lote de origen. Al registrarse restituye las unidades al mismo lote y crea detalle, movimiento y bitácora en una transacción. No elimina el documento original.

La anulación exige motivo, valida estado y actor autorizado, conserva factura y detalle, registra `AnulacionFactura` y `Bitacora`, pone `TotalActual` en cero y restituye a cada lote original únicamente las unidades que todavía no habían sido devueltas. No crea existencias desvinculadas del historial.

### Auditoría

Los cambios importantes de usuarios/roles, emisión de factura, cobro, compra/recepción, devolución y anulación guardan actor, fecha, entidad, identificador, operación y descripción. Nunca se incluyen contraseñas ni `PasswordHash` en la bitácora.

## Objetos SQL

El modelo se queda en 31 tablas. Los scripts finales agregan un tipo tabla y una vista; el archivo de procedimientos final crea nueve SP:

- Tipo: `dbo.TipoDetalleDevolucion`.
- Vista: `dbo.vw_VentasDetalleNeto`, calcula unidades e ingreso neto con precios históricos.
- Procedimientos: `sp_ReporteVentas`, `sp_ReporteProductos`, `sp_ReporteSinStock`, `sp_FacturaLotes`, `sp_FacturaAnular`, `sp_DevolucionDisponibles`, `sp_DevolucionCrear`, `sp_DevolucionListar` y `sp_DevolucionDetalle`.
- Se conserva `fn_EmpleadoNombreCompleto` de Fase I y las vistas anteriores de usuarios/productos/variantes.

Los nueve SP anteriores son usados por `Fase2Datos` para reportes, consulta de trazabilidad y el ciclo de devolución/anulación. No se agregan funciones de proyecto final sin consumidor.

## Instalación y ejecución

Para una base nueva, conectar SSMS a `.\SQLEXPRESS` (o a la instancia local configurada) y ejecutar cada archivo completo en orden. No ejecutar `00` sobre una base de datos que se deba conservar.

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
13. Configurar la cuenta inicial Gerente desde la aplicación, si se necesita cargar la demo.
14. Opcional: `database/scripts/12_Datos_Demostracion_Fase2.sql` para datos comerciales reproducibles. No contiene usuarios, contraseñas ni hashes.
15. `database/scripts/13_Types_ProyectoFinal.sql`
16. `database/scripts/14_Views_ProyectoFinal.sql`
17. `database/scripts/15_Procedures_ProyectoFinal.sql`
18. `database/scripts/16_Sincronizar_Procedimientos.sql`

En una instalación de fase anterior, revisar objetos y ejecutar solo los scripts que falten; no reconstruir ni repetir `CREATE` sobre objetos instalados. El script 16 actualiza seis procedimientos existentes y requiere los scripts 03 y 10. El script 12 es opcional y solo para datos de demostración. Las pruebas SQL 01–04 se ejecutan en QA.

Compilar y abrir la web desde la raíz:

```powershell
dotnet restore .\src\CloudColor.sln
dotnet build .\src\CloudColor.sln -c Release
dotnet run --project .\src\CloudColor.Web -c Release
```

Abrir `http://localhost:5255`. La conexión usa `src/CloudColor.Web/appsettings.json` y configuración local ignorada por Git cuando corresponda. Para WinForms, ejecutar `dotnet run --project .\src\CloudColor.Presentacion -c Release`.

## Pruebas y verificación

Las suites automatizadas crean una base temporal con nombre QA, instalan 00–11 y 13–16, ejecutan pruebas SQL 01–04 y eliminan esa base al terminar. El script de datos 12 no forma parte de la suite automática. Durante este corte se verificó:

- `dotnet restore .\src\CloudColor.sln`: correcto.
- `dotnet build .\src\CloudColor.sln -c Release --no-restore --nologo -warnaserror`: 0 errores, 0 advertencias.
- `dotnet run --project .\tests\CloudColor.Pruebas -c Release --no-build`: 116 comprobaciones correctas, incluidas pruebas de WinForms/Negocio, Fase II, proyectos finales y SQL 01–04.
- La ejecución `--concurrencia` completó 125 comprobaciones; confirmó venta, facturación de cotización, cobro y recepción concurrentes bajo bloqueos observados en SQL Server.
- `dotnet run --project .\tests\CloudColor.Web.Pruebas -c Release --no-build`: 164 comprobaciones HTTP correctas.
- Ambas suites confirmaron que la base original seguía con 31 tablas y 103 filas observadas, sin cambio de su huella de contenido; las bases temporales se eliminaron.
- Navegador Edge/Playwright: 85 páginas de 17 rutas a cinco tamaños (1920×1080, 1440×900, 1366×768, 768×1024 y 390×844), sin overflow horizontal, sin errores de consola/CSP; movimientos reducidos, roles, modal, búsqueda, Top 10, carrito y cobro comprobados. Las capturas finales se guardan en `docs/final/evidencias/`.
- Se guardaron 102 capturas PNG en `docs/final/evidencias/`, incluidas las cinco resoluciones por ruta y pantallas principales a 1440 px.

La suite visual necesita el navegador Edge y Playwright disponible en `tests/CloudColor.Web.Browser`. Para iniciar su servidor temporal:

Defina `CLOUDCOLOR_QA_PASSWORD` localmente para el proceso que ejecuta las suites, sin escribir su valor en el repositorio. El mismo valor se usa en la suite del navegador contra las cuentas QA temporales.

```powershell
dotnet run --project .\tests\CloudColor.Web.Pruebas -c Release --no-build -- --visual
npm run test:ux --prefix tests/CloudColor.Web.Browser
```

El proceso imprime la ubicación exacta de un archivo `.stop`; crearlo solo después de terminar las pruebas de navegador para que QA cierre el servidor y retire la base temporal.

## Guía de presentación

El recorrido sugerido está en [Guia_Presentacion.md](Guia_Presentacion.md). Inicia con login y tablero, muestra una compra y su recepción, inventario PEPS, cotización/factura/cobro/impresión y termina con devolución, anulación, reportes, consulta y bitácora. Usa exclusivamente cuentas y datos de una instalación de demostración que la persona esté autorizada a utilizar.

## Límites y revisión

El documento fiscal es una representación imprimible de la factura académica; no implementa una integración fiscal externa. El hash de contraseñas se genera/verifica en C# y SQL conserva el verificador, de acuerdo con la decisión de seguridad vigente. El cambio debe revisarse en la rama `feat/proyecto-final`; no se hizo merge a `main`.
