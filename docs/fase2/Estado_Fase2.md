# Estado de Fase II — CloudColor

Verificación: 12 de octubre de 2026. Rama: `feat/fase2`. Fase I está validada e integrada a main; esta entrega conserva su código funcional y no realiza el merge de Fase II.

## Alcance y estructura conservada

Se implementan variantes, asociación proveedor/variante, bodegas y ubicaciones, cotizaciones, emisión directa o desde cotización, PEPS, cobros, órdenes, recepciones, inventario y vista previa de factura. Se continuó el avance existente.

El DBML y los scripts 00–05 no se modificaron. Permanecen 31 tablas, 182 columnas, 31 PK y 43 FK. No se agregaron tablas Stock ni roles nuevos. Los tipos tabla son parámetros, no entidades nuevas. Se conserva el índice UNIQUE filtrado de Factura.CotizacionID: permite muchas ventas directas con NULL y como máximo una factura por cotización.

Presentacion → Negocio → Datos → SQL Server; Entidades contiene los objetos transportados entre capas. Los formularios no contienen SQL. Datos utiliza SqlParameter con tipos, tamaños, precisión y escala explícitos, y parámetros Structured para TVP. No se concatena entrada del usuario para construir SQL de la aplicación. El ejecutor de pruebas construye exclusivamente identificadores propios de bases desechables y nombres de tablas obtenidos de metadatos.

## Instalación y ejecución

En una instalación nueva, ejecutar completos y en orden los scripts 00, 01, 02, 03, 04, 05, 06, 07, 08, 09, 10 y 11 desde SSMS. Detenerse ante cualquier error. Ejecutar los tres archivos database/tests en una base de pruebas después de la instalación.

Para una base con Fase I instalada, ejecutar solo 06 → 07 → 08 → 09 → 10 → 11, una vez. Son scripts CREATE; no son migraciones repetibles. No volver a ejecutar 01 ni reconstruir una base que tenga datos. En una instalación parcial, revisar los objetos presentes y aplicar solamente los bloques pendientes o ALTER PROCEDURE/ALTER VIEW para actualizar definiciones existentes. Conservar SET ANSI_NULLS ON y SET QUOTED_IDENTIFIER ON. No reemplazar automáticamente los tipos tabla.

En `.\SQLEXPRESS/CloudColorDB` ya existen los cuatro TVP y se verificó que las dos vistas y los 30 procedimientos de Fase II coinciden con los archivos finales. No se ejecutaron scripts de reconstrucción sobre esa base durante esta validación.

Desde PowerShell en la raíz:

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project src/CloudColor.Presentacion -c Release --no-build
```

Se conserva conexion.json y la prioridad opcional de conexion.local.json. No se distribuyen cuentas ni contraseñas iniciales. Usar las cuentas reales existentes; en una instalación vacía la pantalla inicial crea el primer Gerente, como en Fase I.

## Objetos SQL de Fase II

| Script | Objetos |
|---|---|
| 06_Types_Fase2.sql | 4 tipos tabla |
| 07_Views_Fase2.sql | 2 vistas |
| 08_Procedures_Catalogos_Fase2.sql | 16 procedimientos |
| 09_Procedures_Cotizacion.sql | 4 procedimientos |
| 10_Procedures_Facturacion.sql | 5 procedimientos |
| 11_Procedures_Compras.sql | 5 procedimientos |

Los TVP son:

- TipoDetalleFactura: variante, cantidad y precio; se utiliza en cotización y emisión directa.
- TipoPagoFactura: medio, monto y referencia; una fila por medio, hasta tres medios distintos.
- TipoDetalleOrdenCompra: variante, cantidad solicitada y costo acordado.
- TipoDetalleRecepcion: línea de orden, cantidad recibida, costo real, ubicación, lote proveedor y vencimiento opcionales.

Las PK de estos tipos impiden repetir la misma variante, medio o línea en el mismo envío. Se transportan mediante DataTable/SqlParameter; no se introducen JSON ni XML en SQL.

Las vistas son vw_VariantesDetalle (catálogos y estados de variante/producto/presentación/color) y vw_InventarioActual (existencia por ingreso, producto, presentación, color, SKU, lote interno/proveedor, bodega, ubicación, fecha, cantidad y costo). Ambas se utilizan desde procedimientos consumidos por la aplicación.

Inventario completo de procedimientos:

- **08_Procedures_Catalogos_Fase2.sql**: `sp_AutorizarFase2`, `sp_ProductoVarianteListar`, `sp_ProductoVarianteGuardar`, `sp_BodegaListar`, `sp_BodegaGuardar`, `sp_UbicacionListar`, `sp_UbicacionGuardar`, `sp_ProveedorProductoListar`, `sp_ProveedorProductoGuardar`, `sp_ColorCrear`, `sp_MedioPagoListar`, `sp_InventarioListar`, `sp_ProductoVarianteCambiarActivo`, `sp_BodegaCambiarActivo`, `sp_UbicacionCambiarActivo`, `sp_ProveedorProductoCambiarActivo`.
- **09_Procedures_Cotizacion.sql**: `sp_CotizacionCrear`, `sp_CotizacionConfirmar`, `sp_CotizacionListar`, `sp_CotizacionDetalle`.
- **10_Procedures_Facturacion.sql**: `sp_FacturaGenerar`, `sp_FacturaCobrar`, `sp_FacturaListar`, `sp_FacturaDetalle`, `sp_FacturaPagos`.
- **11_Procedures_Compras.sql**: `sp_OrdenCompraCrear`, `sp_OrdenCompraListar`, `sp_OrdenCompraDetalle`, `sp_RecepcionCompraRegistrar`, `sp_RecepcionCompraListar`.

No se añaden funciones ni triggers. La función y las dos vistas de Fase I siguen en uso y se vuelven a comprobar.

## Operaciones y transacciones

**Variantes y catálogos.** Producto mantiene descripción, marca, categoría y opciones; ProductoVariante mantiene presentación, color opcional, SKU, precio y stock mínimo. Su formulario permite listar, buscar, crear, seleccionar, editar y activar/desactivar. Usa combos, sin introducir IDs. Una variante con historial no puede cambiar de producto/presentación/color. ProveedorProducto evita asociaciones duplicadas, incluidas inactivas; estas se reactivan. Las órdenes solo aceptan variantes activas asociadas al proveedor. Bodega y Ubicacion tienen mantenimiento y estado; una ubicación con lotes no se traslada a otra bodega.

**Cotización.** Seleccionar cliente, agregar variantes y cantidades; el precio actual se sugiere y se guarda en DetalleCotizacion como precio histórico. Guardar deja PENDIENTE; confirmar exige que siga pendiente y no esté vencida. Facturar exige CONFIRMADA y conserva cliente, cantidades y precios del documento. Al emitir pasa a FACTURADA. No reserva ni descuenta existencias antes de la emisión.

**Facturación.** sp_FacturaGenerar valida usuario, cliente, variantes, cantidades, stock y cotización. Inserta Factura y DetalleFactura, distribuye salidas entre lotes, actualiza CantidadDisponible, registra DetalleFacturaLote y movimientos, actualiza la cotización y Bitacora. Todo pertenece a la misma transacción. La venta directa conserva CotizacionID=NULL; no crea cotizaciones artificiales. El total suma subtotales redondeados por línea a dos decimales. La factura queda PENDIENTE_PAGO; si el importe es cero, queda PAGADA sin introducir pagos de monto cero, dado que el modelo permite precios cero y exige pagos positivos.

**PEPS.** El procedimiento usa WHILE por variante y otro WHILE hasta completar la salida. Con Producto.AplicaPEPS activo, selecciona por FechaIngreso ASC y LoteInventarioID ASC como desempate. Una venta de 12 sobre lotes de 5 y 10 consume 5 y 7, dejando 0 y 3. Para productos que no aplican PEPS se mantiene una asignación determinista por LoteInventarioID. Incluso sin NumeroLoteProveedor existe un lote interno por cada ingreso. Cada salida se registra con cantidad positiva en DetalleFacturaLote y negativa como SALIDA_VENTA en MovimientoInventario. El UPDATE exige disponibilidad suficiente y nunca admite stock negativo.

**Pagos.** sp_FacturaCobrar admite Efectivo, Cheque y Tarjeta activos. Cada monto debe ser positivo y la suma debe igualar TotalActual. Cheque y Tarjeta exigen referencia. Se registra UsuarioCajeroID separado de UsuarioEmisorID, se marca PAGADA y se registra Bitacora. Un segundo cobro se rechaza aunque se intente desde otra sesión. El formulario permite quitar una fila para cambiar su distribución antes de registrar el cobro completo.

**Compras.** Seleccionar proveedor filtra las variantes asociadas. Se guardan cantidades solicitadas y costos acordados. La orden comienza PENDIENTE; consultar muestra solicitada, recibida y pendiente por línea.

**Recepción.** Seleccionar la orden y sus líneas pendientes; capturar cantidad, costo real, ubicación activa, fecha de ingreso y lote/vencimiento cuando corresponda. sp_RecepcionCompraRegistrar valida el acumulado recibido y crea RecepcionCompra, DetalleRecepcionCompra, LoteInventario y MovimientoInventario ENTRADA_COMPRA. El costo real del ingreso puede ser distinto del costo acordado de la orden. Lote proveedor es obligatorio para productos con ManejaLote activo; en los demás puede ser NULL. Cada línea admite una ubicación/lote por recepción: para distribuir entre ubicaciones, usar recepciones separadas. La orden de 100 admite 75 (PARCIAL) y 25 (COMPLETA); recibir 30 tras las 75 se rechaza sin insertar una recepción ni alterar existencias.

**Inventario.** No existe tabla Stock nueva. La vista expone CantidadDisponible por lote y conserva filas agotadas para trazabilidad. La pantalla permite buscar producto o SKU y muestra el lote interno, producto/presentación/color, lote proveedor, bodega, ubicación, ingreso, disponible y costo. Hay desplazamiento horizontal para columnas extensas.

**Impresión.** PrintDocument y PrintPreviewDialog generan la factura con CloudColor, número, fecha, cliente, empleado emisor, SKU/producto/presentación/color, cantidad, precio, subtotal, total y medios de pago. El documento pagina detalles largos sin perder líneas. No hay librerías de impresión externas. Windows necesita una impresora válida incluso para la vista previa. La prueba utiliza PreviewPrintController y no imprime físicamente.

Los cinco procedimientos transaccionales son sp_CotizacionCrear, sp_FacturaGenerar, sp_FacturaCobrar, sp_OrdenCompraCrear y sp_RecepcionCompraRegistrar. Utilizan BEGIN TRANSACTION, COMMIT, TRY/CATCH, ROLLBACK y THROW. Si ya existe una transacción del llamador, utilizan un SAVE TRANSACTION para que los rechazos de prueba no descarten la preparación anterior; si la transacción queda irrecuperable, se revierte completa. La confirmación de cotización es un UPDATE condicional único.

Para concurrencia, emisión y recepción bloquean las variantes en orden creciente de ID usando UPDLOCK/HOLDLOCK. La cotización, factura por cobrar y orden por recibir también se bloquean hasta terminar su transacción. El índice UNIQUE filtrado continúa siendo la protección final contra duplicar cotizaciones. No se emplean CTE, CROSS APPLY ni cursores para sustituir WHILE.

## Pantallas y permisos

Pantallas nuevas: ProductoVarianteForm, ColorForm (auxiliar), BodegaForm, UbicacionForm, ProveedorProductoForm, CotizacionForm, FacturacionForm, PagoForm, OrdenCompraForm, RecepcionCompraForm e InventarioForm. DocumentoForm y VentaForm comparten presentación sin cambiar las cuatro capas. MenuPrincipalForm conserva todos los accesos de Fase I.

| Operación | Digitador | Cajero | Gerente |
|---|---|---|---|
| Mantenimientos de Fase I | Sí | No | Sí |
| Usuarios y roles | No | No | Sí |
| Variantes, bodegas, ubicaciones y asociaciones | Sí | No | Sí |
| Cotizaciones y emisión de facturas | Sí | No | Sí |
| Órdenes y recepciones | Sí | No | Sí |
| Inventario | Sí | No | Sí |
| Consultar/imprimir facturas | Sí | Sí | Sí |
| Cobrar y registrar medios | No | Sí | Sí |

La matriz mantiene al Gerente con acceso operativo y de supervisión. Se conservan exactamente los tres perfiles. UI guía los accesos; Negocio revalida la sesión y cada procedimiento valida el actor y el estado de usuario, empleado y rol. No se depende únicamente de ocultar botones. La cuenta Windows que administra directamente SQL Server sigue fuera del perímetro de autorización de la aplicación.

PBKDF2-HMAC-SHA256 permanece intacto: 600000 iteraciones, salt aleatorio, comparación de tiempo constante y PasswordHash. La contraseña original no se envía a SQL Server.

## Pruebas y evidencia

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project tests/CloudColor.Pruebas -c Release --no-build -- --concurrencia
```

EntornoPruebas crea una base CloudColor_QA_<GUID>, instala 00–11 y ejecuta las tres suites SQL. Las pruebas C# de Fase I/II utilizan transacciones ambientales revertidas; las carreras usan datos confirmados solo en esa base desechable. Al terminar se compara el contenido original mediante conteos y huellas, sin imprimir datos sensibles, y se elimina la base QA. Requiere CREATE/DROP DATABASE y, para observar bloqueos, visibilidad de sesiones. No ejecutar mientras se editan datos reales. Una terminación forzada puede impedir la limpieza: revisar el nombre exacto de la base temporal antes de intervenir.

| Verificación | Resultado observado |
|---|---|
| Build Release con advertencias tratadas como errores | 0 errores, 0 advertencias |
| Instalación desde cero | Scripts 00–11 correctos en base temporal |
| SQL 01_TestStructure | Correcto; 31 tablas/PK, 43 FK e integridad |
| SQL 02_TestFase1 | Correcto; procedimientos, función y vistas |
| SQL 03_TestFase2 | Correcto; operaciones, rechazo de errores, PEPS, pagos y recepción 100/75/25 |
| C# total | 110 comprobaciones correctas |
| Regresión Fase I | 49 comprobaciones correctas, incluida Nueva marca, login, usuarios, roles y PBKDF2 |
| Fase II e interfaz | 52 comprobaciones correctas |
| Concurrencia y comprobaciones asociadas | 9 comprobaciones; 4 carreras con bloqueo observado |
| Base original | 31 tablas y 31 filas, contenido idéntico antes/después |
| Limpieza | Base QA eliminada; ninguna base CloudColor_QA_ restante |

Carreras realizadas con dos conexiones reales:

1. Dos ventas de 10 sobre 15 unidades: solo una se registra; saldo 5. También se comprueba el desempate PEPS por ID con fechas iguales.
2. Dos facturas de la misma cotización: solo una se registra.
3. Dos cobros de la misma factura: solo un pago queda persistido.
4. Dos recepciones de 7 sobre una orden de 10: solo una se registra; pendiente 3.

SQL también prueba emisor Digitador/cobrador Cajero, Cheque con referencia obligatoria, Efectivo + Tarjeta, stock insuficiente sin modificaciones, rechazo de cotización no confirmada/reutilizada y correspondencia de movimientos con existencias. Los escenarios de fallo validan que los rechazos no dejan cabeceras, pagos ni movimientos parciales; no constituyen una simulación de cortes eléctricos ni de todos los fallos posibles del motor.

Las pruebas C# accionan controles WinForms reales: combos, crear/editar/activar variantes, selección de otra factura y cambio de su detalle, búsqueda, mensajes de error, filtros al guardar, navegación desde el menú y pagos. Generan vista previa real de PrintDocument y prueban paginación. No equivalen a la aprobación manual del usuario.

Capturas: `%TEMP%/CloudColor-Pruebas`. Se revisaron visualmente catálogos, compras/recepciones, cotización, factura/cobro, inventario, menú y factura impresa. Para la entrega, tomar evidencias con datos autorizados:

- 11-variantes y 14-proveedor-variante: separación del producto y asociación al proveedor.
- 15/16-orden y 17/18-recepción: cantidades solicitadas, recibidas y pendientes; repetir manualmente el ejemplo 100/75/25 y el rechazo de 30.
- 19/20-cotización y 21/22-factura: confirmación y precio histórico.
- 23-cajero-facturas y 24-pagos-multiples: permisos y distribución exacta del total.
- 25-factura-impresa y 26-inventario: contenido de factura, ubicación/costo y saldos por lote.
- Resultado de la consola de pruebas y consulta SQL de salidas 5 + 7: evidencia de PEPS y concurrencia.

## Datos permanentes conservados

| Tabla | Filas |
|---|---:|
| CategoriaProducto | 6 |
| MedioPago | 3 |
| Presentacion | 8 |
| Rol | 3 |
| Cliente | 1 |
| Proveedor | 1 |
| Producto | 1 |
| Marca | 2 |
| Empleado | 3 |
| Usuario | 3 |

Son 20 filas de catálogos y 11 registros existentes de la presentación de Fase I. Las otras 21 tablas permanecen vacías. No se agregaron cuentas ni datos comerciales permanentes para probar Fase II; la comprobación comparó contenido, no solamente cantidades.

## Correcciones durante el cierre

- El avance sin commit estaba sobre main; se trasladó a feat/fase2 sin descartar archivos ni modificar el historial de Fase I.
- La prueba de estructura contaba también las PK de TVP; ahora cuenta solo las PK de las 31 tablas físicas.
- La suite anterior exigía una base sin usuarios. Ahora crea un entorno temporal y permite probar configuración inicial conservando las cuentas reales.
- Se mantienen ANSI_NULLS y QUOTED_IDENTIFIER ON en los scripts nuevos para la operación con el índice filtrado de Factura.
- Se completó el caso SQL obligatorio de recepción 100/75/25 con rechazo de 75+30, costo real y ubicación, y la validación de Cheque/referencia/emisor/cajero.
- Al guardar documentos se limpia el filtro previo para que no oculte el registro recién creado. En ventas se selecciona explícitamente la nueva cotización/factura.
- Los mensajes de error se limpian al corregir la captura; los encabezados de catálogos se ajustan después de recargar; el editor de pagos no muestra una fecha todavía inexistente.
- Se comprueba la existencia de impresora válida antes de abrir la vista previa. Se agregaron regresiones de búsqueda, selección y navegación.

## Archivos de la entrega

- SQL: scripts 06, 07, 08, 09, 10 y 11 nuevos; tests/03_TestFase2.sql nuevo y tests/01_TestStructure.sql ajustado.
- Entidades: src/CloudColor.Entidades/Fase2.cs nuevo.
- Datos: src/CloudColor.Datos/Fase2Datos.cs nuevo.
- Negocio: Fase2Servicio.cs nuevo y Aplicacion.cs ampliado.
- Presentacion: AlmacenForms.cs, ComprasForms.cs, FacturaImpresion.cs, ProductoVarianteForm.cs, UiFase2.cs y VentasForms.cs nuevos; MenuPrincipalForm.cs ampliado.
- Pruebas C#: Fase2.cs, Concurrencia.cs y EntornoPruebas.cs nuevos; Program.cs ampliado.
- Documentación: este documento nuevo y README.md actualizado.

El archivo local database/scripts/scriptsbasededatosiicloudcolor.zip no forma parte de esta entrega y se conserva sin modificar ni incorporar a Git.

## Pendientes para la entrega

Revisión manual del usuario sobre los flujos documentados y aceptación académica de Fase II antes de integrar feat/fase2 a main. Comprobar la vista previa en el equipo de presentación y, si se requiere papel, su impresora física; aquí no se enviaron trabajos de impresión. Las capturas de pruebas son temporales y deben reemplazarse por las evidencias elegidas por el equipo.

No se identificó una necesidad de cambiar el modelo. No quedan fallos en las verificaciones ejecutadas. La aprobación humana no se sustituye por las pruebas automáticas. Reportes, devoluciones y anulaciones quedan para su etapa; no se agregan a Fase II.
