# Matriz de trazabilidad de CloudColor

Esta matriz relaciona requisitos implementados con sus componentes principales. Las pruebas citadas se ejecutan contra una base QA temporal; su resultado de cada revisión se registra en el checklist, no se deduce de la existencia del código.

| Requisito | Módulo | Pantalla | Capa Negocio | Capa Datos | Objeto SQL principal | Prueba | Estado |
|---|---|---|---|---|---|---|---|
| Autenticar y restringir según rol | Seguridad | Login, usuarios y roles | SeguridadServicio | SeguridadDatos | sp_UsuarioSesion, sp_UsuarioObtenerPorNombreUsuario | C#, HTTP, acceso directo por rol | Implementado; consultar checklist |
| Mantener clientes, proveedores, productos y empleados | Catálogos | Clientes, Proveedores, Productos, Empleados | ClienteServicio, ProveedorServicio, ProductoServicio, EmpleadoServicio | Datos correspondientes | sp_ClienteListar, sp_ProveedorListar, sp_ProductoListar, sp_EmpleadoListar y sus procedimientos de mantenimiento | C#, HTTP y navegador | Implementado; consultar checklist |
| Mantener variantes, bodegas, ubicaciones y asociaciones | Catálogos e inventario | Variantes, Bodegas, Ubicaciones, Proveedor / variante | Fase2Servicio | Fase2Datos | sp_ProductoVarianteGuardar, sp_BodegaGuardar, sp_UbicacionGuardar, sp_ProveedorProductoGuardar | C#, HTTP y navegador | Implementado; consultar checklist |
| Crear y confirmar cotización y facturarla una sola vez | Ventas | Cotizaciones y Facturas | Fase2Servicio | Fase2Datos | sp_CotizacionCrear, sp_CotizacionConfirmar, sp_FacturaGenerar | SQL, C#, HTTP, navegador y concurrencia | Implementado; consultar checklist |
| Facturar directamente y asignar lotes PEPS | Facturación e inventario | Facturas e Inventario | Fase2Servicio | Fase2Datos | sp_FacturaGenerar, sp_FacturaLotes, vw_InventarioActual, TipoDetalleFactura | SQL, C#, navegador y concurrencia | Implementado; consultar checklist |
| Cobrar con uno o varios medios | Cobros | Cobros y factura | Fase2Servicio | Fase2Datos | sp_FacturaCobrar, sp_FacturaPagos, TipoPagoFactura | SQL, C#, HTTP, navegador y concurrencia | Implementado; consultar checklist |
| Comprar y recibir cantidades parciales o completas | Compras | Órdenes y recepciones | Fase2Servicio | Fase2Datos | sp_OrdenCompraCrear, sp_RecepcionCompraRegistrar, TipoDetalleOrdenCompra, TipoDetalleRecepcion | SQL, C#, HTTP, navegador y concurrencia | Implementado; consultar checklist |
| Consultar existencias por lote | Inventario | Inventario | Fase2Servicio | Fase2Datos | sp_InventarioListar, vw_InventarioActual | SQL, C#, HTTP y navegador | Implementado; consultar checklist |
| Devolver hasta la cantidad vendida y restituir lotes | Devoluciones | Devoluciones | Fase2Servicio | Fase2Datos | sp_DevolucionDisponibles, sp_DevolucionCrear, TipoDetalleDevolucion | SQL, C#, HTTP y concurrencia | Implementado; consultar checklist |
| Anular conservando historial y restituyendo lo pendiente | Facturación | Detalle de factura y anulación | Fase2Servicio | Fase2Datos | sp_FacturaAnular, sp_FacturaLotes | SQL, C#, HTTP y concurrencia | Implementado; consultar checklist |
| Consultar ventas, productos y faltantes con precios históricos | Reportes | Ventas, productos e inventario | Fase2Servicio | Fase2Datos | sp_ReporteVentas, sp_ReporteProductos, sp_ReporteSinStock, vw_VentasDetalleNeto | SQL, C#, HTTP y navegador | Implementado; consultar checklist |
| Consultar e imprimir una factura | Facturación y reportes | Consulta de factura e impresión | Fase2Servicio | Fase2Datos | sp_FacturaListar, sp_FacturaDetalle, sp_FacturaPagos, sp_FacturaLotes | C#, HTTP y navegador | Implementado; consultar checklist |

Los tipos tabla permiten enviar varias filas estructuradas y tipadas a SQL Server en una sola llamada. La función `fn_EmpleadoNombreCompleto` y las vistas complementan los procedimientos; los objetos usados por cada flujo pueden consultarse en los scripts SQL del repositorio.
