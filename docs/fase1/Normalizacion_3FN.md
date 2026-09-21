# Normalización del modelo CloudColor

## Alcance y criterio

El modelo aprobado contiene 31 tablas. Se analiza la información de una empresa que compra y vende pinturas y artículos relacionados; no se agregan entidades ni se cambian claves. La normalización se justifica a partir del significado de los atributos, no solamente de la presencia de un identificador numérico.

Una **clave primaria (PK)** identifica una fila: `Producto.ProductoID` distingue un producto de los demás. Una **clave foránea (FK)** conserva una referencia válida: `Producto.MarcaID` debe apuntar a una marca existente. Las FK evitan referencias inexistentes, pero no comprueban por sí solas reglas como una devolución acumulada mayor que la venta.

## Primera forma normal (1FN)

Cada columna representa un valor del dominio elegido y las filas no contienen grupos repetitivos de productos, pagos o proveedores.

- Una factura no contiene columnas Producto1, Producto2 y Producto3. Sus artículos están en filas de `DetalleFactura`, relacionadas por `FacturaID`.
- Una factura pagada con efectivo y tarjeta tiene dos filas de `PagoFactura`, no una lista de medios de pago en una columna.
- Una orden recibida en varias entregas conserva varias filas de `RecepcionCompra`; `DetalleRecepcionCompra` identifica qué cantidades llegaron en cada entrega.
- Los colores y presentaciones comercializados se representan en `ProductoVariante`, no en listas dentro del nombre del producto.

`Bitacora.DatosAnteriores` y `DatosNuevos` se tratan como textos de evidencia histórica. No sustituyen las tablas operativas ni sirven como listas de entidades para consultas relacionales. Su contenido y formato deberán definirse al implementar la auditoría.

## Segunda forma normal (2FN)

Además de cumplir 1FN, los atributos no clave deben depender de toda una clave candidata, no solamente de una parte. Tener PK de una columna simplifica el análisis de esa PK, pero no elimina la necesidad de revisar las combinaciones que también identifican registros.

- En `ProveedorProducto`, la combinación única `(ProveedorID, ProductoVarianteID)` identifica una asociación. `Activo` describe esa asociación completa. El nombre del proveedor está en `Proveedor` y los datos de la variante están en `ProductoVariante`.
- En `DetalleFacturaLote`, `(DetalleFacturaID, LoteInventarioID)` es único. `CantidadSalida` corresponde a la cantidad tomada de ese lote para esa línea concreta; no es una propiedad solamente del lote o solamente de la factura.
- En `Ubicacion`, `(BodegaID, Codigo)` identifica la ubicación dentro de una bodega. Una misma etiqueta puede repetirse en bodegas diferentes. La dirección de la bodega se conserva en `Bodega`, sin repetirla por cada ubicación.
- En `DetalleRecepcionCompra`, la combinación única de recepción y detalle de orden identifica la cantidad recibida de una línea en una entrega.

`ProductoVariante` tiene PK propia, SKU único y unicidad de producto, presentación y color. Como ColorID admite NULL, esa combinación se describe como una restricción de unicidad del diseño, no como una clave candidata obligatoria en el sentido relacional estricto. SQL Server impide repetir la combinación incluso cuando el color es NULL.

## Tercera forma normal (3FN)

Además de cumplir 2FN, deben evitarse dependencias transitivas de atributos no clave respecto de la clave. CloudColor separa descripciones que pertenecen a entidades diferentes.

Ejemplos de dependencias funcionales aplicadas al modelo:

| Relación | Dependencia y decisión |
|---|---|
| Producto | `ProductoID → Nombre, Descripcion, CategoriaProductoID, MarcaID, ManejaLote, AplicaPEPS, Activo`. El nombre de la marca no se repite en Producto. |
| Marca | `MarcaID → Nombre, Activo`; Nombre también es único. Cambiar la marca se realiza en su catálogo. |
| Usuario | `UsuarioID → EmpleadoID, RolID, NombreUsuario, PasswordHash, Activo`. Los nombres del empleado y la descripción del rol no se copian a Usuario. |
| Empleado | `EmpleadoID → CodigoEmpleado, Nombres, Apellidos, Telefono, Correo, Activo`. CodigoEmpleado también es único. |
| Rol | `RolID → Nombre, Descripcion, Activo`. El permiso funcional se interpreta a partir de los tres roles acordados. |
| Cliente | `ClienteID → TipoCliente, NombreRazonSocial, NIT, Telefono, Correo, Direccion, Activo`. No se supone que NIT sea clave: el DBML no lo declara único. |
| LoteInventario | `LoteInventarioID → DetalleRecepcionCompraID, UbicacionID, NumeroLoteProveedor, fechas, cantidades, CostoUnitario`. La variante se conoce por el detalle recibido y el detalle de orden, sin repetir ProductoVarianteID en el lote. |

Separar `Marca`, `CategoriaProducto`, `Presentacion`, `Color` y `Rol` evita repetir sus descripciones en muchos registros y permite referenciarlos con FK. Cada catálogo tiene identidad y atributos propios. Una presentación corresponde a una variante comercial; no debe duplicarse su unidad de medida en cada producto. El color puede no aplicar y por eso la FK de la variante es opcional.

La FK no obliga a copiar al hijo el nombre del padre. Los JOIN y las vistas permiten mostrar nombres de marca, categoría, empleado y rol sin almacenarlos nuevamente.

## Encabezados y detalles

`OrdenCompra`, `RecepcionCompra`, `Cotizacion`, `Factura` y `Devolucion` contienen información común del documento, como fecha, responsable y referencias. Sus detalles representan las líneas.

Por ejemplo, `Cotizacion.ClienteID` identifica al cliente de toda la cotización. Repetirlo en cada `DetalleCotizacion` permitiría que distintas líneas aparentaran corresponder a clientes distintos. En cambio, cada detalle contiene variante, cantidad y precio de esa línea.

Las recepciones parciales no sobrescriben la cantidad solicitada: cada recepción agrega su evidencia en `DetalleRecepcionCompra`. La comparación entre lo solicitado y la suma recibida pertenece a las reglas transaccionales posteriores.

## Tablas puente

`ProveedorProducto` resuelve la relación muchos a muchos entre proveedor y variante: un proveedor ofrece varias variantes y una variante puede tener varios proveedores.

`DetalleFacturaLote` resuelve la asignación de lotes a las líneas vendidas: una línea puede consumir varios lotes y un lote puede abastecer distintas ventas. Su cantidad es propia de la asociación. Esta tabla conserva el origen de la salida, necesario para PEPS y devoluciones.

`DetalleDevolucion` referencia `DetalleFacturaLote`: identifica la salida concreta que se devuelve. La unicidad dentro de una devolución no impide otras devoluciones del mismo artículo; el límite acumulado se validará mediante procedimientos transaccionales.

## Precios y conservación de historia

`ProductoVariante.PrecioVentaActual` es el precio vigente del catálogo. `DetalleCotizacion.PrecioUnitario` y `DetalleFactura.PrecioUnitario` representan el precio acordado en operaciones particulares. Cambiar el precio actual no debe reescribir documentos anteriores. No son el mismo hecho repetido: pertenecen a momentos y operaciones distintos.

De forma semejante, `DetalleOrdenCompra.CostoUnitarioOrden` registra el costo acordado y `LoteInventario.CostoUnitario` conserva el costo de adquisición asignado al lote. No se presume que deba deducirse del precio de venta actual.

`Factura.TotalOriginal` conserva el importe original y `TotalActual` puede llegar a cero tras una anulación. `CantidadDisponible` mantiene el saldo del lote. Son valores almacenados relacionados con cálculos y operaciones históricas: su consistencia requiere actualizaciones transaccionales. No se afirma que toda redundancia desaparezca por tener PK; estas decisiones del modelo aprobado deben documentarse y controlarse.

`Activo` evita eliminar catálogos o personas con referencias históricas. No se utiliza eliminación en cascada en documentos comerciales. La conservación del precio histórico no implica conservar automáticamente todas las versiones de nombres o direcciones: el modelo no tiene tablas de versiones para esos atributos.

## Cardinalidad confirmada y límites de la justificación

Una cotización genera como máximo una factura. Una factura puede no tener cotización. Se conserva `Factura.CotizacionID` nullable y se usa un índice UNIQUE filtrado para valores no nulos; un UNIQUE sin filtro impediría múltiples filas sin cotización en SQL Server.

La organización del modelo evita los grupos repetitivos, dependencias parciales y duplicaciones transitivas descritos. Esta justificación supone el significado de atributos expuesto aquí; nuevas reglas de negocio podrían revelar otras dependencias. No convierte las restricciones de FK en validaciones de todas las reglas comerciales y no propone cambios al modelo aprobado.
