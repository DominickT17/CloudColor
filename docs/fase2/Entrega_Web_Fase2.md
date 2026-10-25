# Entrega de Fase II — CloudColor Web

Esta guía describe las funciones de Fase II de la aplicación MVC/Razor, extendidas en la rama de revisión `feat/proyecto-final`. Web reutiliza Negocio, Datos, Entidades y los procedimientos de SQL Server 2022 Express; WinForms continúa disponible. El modelo conserva sus 31 tablas.

## Requisitos y solución implementada

| Requisito | Opción de la aplicación | Implementación de base de datos |
|---|---|---|
| Generar cotización a un cliente | Cotizaciones → Nueva cotización; seleccionar cliente y variantes, cantidades y precios; guardar | `sp_CotizacionCrear`, `Cotizacion`, `DetalleCotizacion` |
| Facturar una cotización confirmada | Consultar cotización → Confirmar → Facturar cotización → Emitir factura | `sp_CotizacionConfirmar` y `sp_FacturaGenerar`; conserva precios históricos y rechaza reutilización |
| Facturar sin cotización previa | Facturas → Nueva factura; preparar detalle y revisar total antes de emitir | `sp_FacturaGenerar` con `CotizacionID = NULL`; no crea una cotización artificial |
| SP transaccional y actualización de existencias | Emitir factura | Factura, detalle, asignación PEPS, descuento de lotes, movimientos y bitácora dentro de la misma transacción |
| Medios de pago | Consultar factura → Registrar cobro, o Cobros | `sp_FacturaCobrar`; Efectivo, Cheque y Tarjeta; admite distribución entre medios |
| Impresión | Consultar factura → Vista de impresión → Imprimir factura | Consulta de factura, detalle y pagos; vista para papel o PDF del navegador |
| Orden de compra a proveedor | Órdenes de compra → Nueva orden; cargar variantes del proveedor y guardar | `sp_OrdenCompraCrear`, `OrdenCompra`, `DetalleOrdenCompra` |
| Ingreso completo o parcial | Recepciones o consultar compra → Registrar recepción; seleccionar ubicación y cantidades | `sp_RecepcionCompraRegistrar`; crea lotes y movimientos; calcula PARCIAL/COMPLETA sin exceder lo pendiente |

## Elección de UDT/TVP frente a JSON y XML

Se eligieron tipos tabla definidos por el usuario (`CREATE TYPE ... AS TABLE`), enviados como parámetros tabulares TVP `READONLY`. Son tipos de SQL Server, no nuevas tablas físicas. Los detalles contienen varias filas y columnas conocidas; un TVP permite tiparlas sin convertir texto JSON/XML y mantiene el T-SQL cercano al curso.

- `TipoDetalleFactura`: variantes, cantidades y precios para cotización/facturación.
- `TipoPagoFactura`: medios, montos y referencias.
- `TipoDetalleOrdenCompra`: variantes, cantidades y costos acordados.
- `TipoDetalleRecepcion`: líneas de la orden, cantidades, costos reales, ubicación y lote.

ADO.NET los envía mediante `SqlParameter`, `SqlDbType.Structured` y `TypeName` en `src/CloudColor.Datos/Fase2Datos.cs`. JSON y XML fueron considerados como alternativas de transporte; no se necesitan simultáneamente. No se usan UDT CLR ni serializadores adicionales.

Los SP usan `BEGIN TRANSACTION`, `COMMIT`, `TRY/CATCH`, `ROLLBACK` y `THROW`; admiten una transacción del llamador mediante punto de guardado. Facturación y recepción verifican stock y cantidades bajo bloqueos. La regla de una factura como máximo por cotización mantiene su índice UNIQUE filtrado; muchas facturas directas pueden tener CotizacionID NULL.

## Preparación y ejecución

1. En un equipo nuevo, instalar SQL Server 2022 Express, SSMS y SDK .NET 9. Seguir la configuración de conexión del [README](../../README.md).
2. En una base nueva, ejecutar en SSMS los scripts `00` a `11` en orden. Si ya se instaló Fase I, instalar solamente `06` a `11` que aún no existan. No repetir los CREATE sobre objetos instalados ni reconstruir una base con información que se desea conservar.
3. Iniciar WinForms para configurar el primer Gerente con una contraseña elegida por la persona. Esta configuración inicial es necesaria porque los procedimientos de Fase II autorizan cada operación; el script 12 no crea usuarios y no contiene contraseñas ni hashes.
4. Ejecutar `database/scripts/12_Datos_Demostracion_Fase2.sql` una sola vez en SSMS. Carga los clientes, proveedores, productos y variantes de demostración; prepara bodega, ubicaciones y asociaciones; registra compras y recepciones mediante los SP oficiales; y crea las cotizaciones, facturas, cobro e inventario PEPS. El script comprueba los estados y existencias finales. Si encuentra identificadores de la demostración ya usados, se detiene antes de insertar para evitar duplicados. Las diez capturas están en [evidencias](evidencias/).
5. Compilar y ejecutar desde la raíz:

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project src/CloudColor.Web -c Release --no-build
```

Abrir `http://localhost:5255/Login` para desarrollo local. Gerente y Digitador preparan cotizaciones, emiten facturas y gestionan compras/recepciones. Gerente y Cajero registran cobros. Cajero no puede emitir ni mantener catálogos.

## Recorrido para demostrar Fase II

Usar un entorno de demostración autorizado; los ensayos automáticos utilizan una base desechable.

1. Preparar cliente, proveedor, producto, variante con precio, asociación proveedor/variante, bodega y ubicación.
2. Crear una orden de 100 unidades. Recibir 75: verificar PARCIAL y 25 pendientes. Recibir las otras 25: verificar COMPLETA y 100 recibidas. Una recepción de 30 después de las primeras 75 debe rechazarse sin cambios.
3. Consultar Inventario y verificar los lotes ingresados. Crear una cotización de 2 unidades y confirmar. Emitir su factura: verificar el descuento de 2 unidades. No se puede volver a facturar la misma cotización.
4. Crear una factura directa de 1 unidad sin elegir cotización. El total se revisa en el carrito antes de emitir y la existencia disminuye otra unidad.
5. Registrar cobro exacto, por ejemplo repartido entre Efectivo y Tarjeta. Tarjeta/Cheque requieren referencia. Verificar PAGADA y saldo cero. Un segundo cobro debe rechazarse.
6. Abrir la vista de impresión y guardar PDF o imprimir en el equipo de presentación.

Capturas sugeridas: cotización pendiente/confirmada; factura con su detalle; cobro distribuido y factura pagada; factura imprimible; orden de 100; recepción parcial 75/100; recepción completa 100/100; inventario con lotes. Usar datos de QA, sin contraseñas ni información personal real.

## Verificación reproducible

Para instalar los objetos y preparar la demostración en una base nueva, el orden es `00` → `01` → `02` → `03` → `04` → `05` → `06` → `07` → `08` → `09` → `10` → `11` → configuración inicial del Gerente desde la aplicación → `12` (opcional, solo para la demostración) → `13` → `14` → `15`. Si no se quieren los datos comerciales de demostración, omitir `12`. La configuración inicial genera el verificador PBKDF2 desde C#; los scripts no publican ni insertan un hash predefinido.

```powershell
dotnet run --project tests/CloudColor.Pruebas -c Release --no-build
dotnet run --project tests/CloudColor.Web.Pruebas -c Release --no-build
```

Las suites C# y web instalan scripts 00–11 y 13–15 en bases QA, ejecutan pruebas SQL 01–04 y comprueban que los datos originales se conserven. La carga de demostración 12 se omite en QA. Para navegador responsive, iniciar la suite web con `--visual` y ejecutar `npm run test:ux --prefix tests/CloudColor.Web.Browser` con Node, Playwright local y Edge disponibles. El runner imprime el archivo `.stop` exacto; crearlo al final elimina el servidor y la base temporal. El [estado final](../final/Estado_Proyecto_Final.md) documenta la validación conjunta y la [guía de presentación](../final/Guia_Presentacion.md) describe el recorrido.

La validación automática no sustituye la aceptación del docente ni la prueba con una impresora física. La impresión implementada corresponde al documento académico de la operación; no constituye una integración de facturación fiscal externa. No se realiza merge como parte de esta entrega.

## Validación conjunta más reciente

El estado conjunto probado el 25 de octubre de 2026 está en [Estado_Proyecto_Final.md](../final/Estado_Proyecto_Final.md). En ese corte, Release compiló con cero errores y cero advertencias, pasaron 116 comprobaciones C#/SQL, 125 en la ejecución de concurrencia, 164 HTTP y 85 vistas de navegador responsivas. Las suites eliminaron sus bases QA tras verificar que la base original conservó su huella observada (31 tablas y 103 filas). Las capturas finales se guardan en `docs/final/evidencias/`.

No se modificó la base persistente como parte de estas pruebas. La impresión es una vista académica de factura con opción del navegador para imprimir o guardar como PDF; no es una integración fiscal externa.
