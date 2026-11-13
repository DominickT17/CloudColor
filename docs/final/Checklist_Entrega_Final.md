# Checklist de entrega final

## Revisión del 9 de noviembre de 2026

La tabla siguiente distingue las comprobaciones automatizadas en QA de una revisión exhaustiva de cada botón y combinación. `OK` identifica lo que se comprobó; `PENDIENTE` indica una cobertura de entrega todavía incompleta. Las pruebas de escritura usaron bases temporales; no se cargó la demo en CloudColorDB.

| Módulo | Ruta | Carga | Consultar | Crear/Acción | Editar | Permisos | Responsive | Resultado |
|---|---|---|---|---|---|---|---|---|
| Dashboard | `/Dashboard` | OK QA | OK QA | OK accesos ensayados | No aplica | OK QA roles | OK cinco tamaños | OK |
| Clientes | `/Clientes` | OK QA | OK buscar y limpiar en Edge | OK alta y estado en Edge | OK Edge | OK restricciones QA | OK cinco tamaños | OK |
| Proveedores y productos | `/Proveedores`, `/Productos` | OK QA | OK buscar y limpiar en Edge | OK altas y estado en Edge | OK Edge | OK restricciones QA | OK listas cinco tamaños | OK |
| Empleados, usuarios, roles | `/Empleados`, `/Usuarios`, `/Roles` | OK QA | OK búsqueda y limpieza en Edge | OK empleados, usuarios y estado de roles en Edge | OK Edge | OK acceso directo, POST y restricción de Gerente | OK listas cinco tamaños | OK |
| Variantes y almacenamiento | `/Variantes`, `/Bodegas`, `/Ubicaciones`, `/ProveedorProducto` | OK QA | OK buscar y limpiar en Edge | OK altas y estado en Edge | OK Edge | OK restricciones QA | OK listas cinco tamaños | OK |
| Cotizaciones | `/Cotizaciones` | OK QA | OK QA | OK navegador confirmar y facturar | No aplica | OK restricciones QA | OK cinco tamaños en editor | OK |
| Facturas y cobros | `/Facturas`, `/Cobros` | OK QA | OK QA | OK navegador factura directa, desde cotización y pago combinado; HTTP cheque | No aplica | OK restricciones QA | OK cinco tamaños en vistas ensayadas | OK |
| Compras y recepciones | `/Compras`, `/Recepciones` | OK QA | OK QA | OK navegador 75/25; exceso rechazado por HTTP | No aplica | OK restricciones QA | OK cinco tamaños en vistas ensayadas | OK |
| Inventario y PEPS | `/Inventario` | OK QA | OK filtros combinados en Edge | OK PEPS C#/SQL y HTTP | No aplica | OK restricciones QA | OK cinco tamaños | OK |
| Devolución y anulación | `/Devoluciones`, `/Facturas/Ver/{id}` | OK QA | OK QA | CORREGIDO devolución parcial con dos lotes y anulación en Edge; OK concurrencia | No aplica | OK restricciones QA | OK cinco tamaños en vistas ensayadas | CORREGIDO |
| Reportes | `/Reportes/Ventas`, `/Reportes/Productos`, `/Reportes/Inventario`, `/Reportes/Factura` | OK QA | OK fechas y resultados ensayados | OK Top 5/10/todos y CSV en Edge | No aplica | OK acceso directo QA | OK cinco tamaños en vistas ensayadas | OK |
| Búsqueda global | `/Buscar` | OK QA | OK siete grupos en Edge y HTTP | No aplica | No aplica | OK tres perfiles | OK búsqueda en cinco tamaños | OK |

El Release recompilado registró 0 errores y 0 advertencias. SQL 01–04 y la suite C#/concurrencia terminaron con 130 comprobaciones, incluidos seis conflictos concurrentes; CloudColorDB mantuvo 31 tablas y 119 filas con la misma huella de contenido. La regresión web completó 167 comprobaciones HTTP y las suites Edge de flujos, interactividad, UX, menú, catálogos y búsqueda pasaron. Un recorrido adicional creó, buscó, limpió la búsqueda, editó, desactivó y activó clientes, proveedores, productos, bodegas, ubicaciones, variantes, asociaciones, empleados y usuarios desde Edge. Se corrigió y verificó el formulario de devolución para una factura con dos lotes; también se accionaron los 23 enlaces del menú a cinco tamaños. La tabla registra las pruebas y casos ejecutados; no pretende enumerar todas las combinaciones posibles de datos.

## Revisión anterior del 4 de noviembre

Revisión del 4 de noviembre de 2026, en progreso. `Real` significa consulta en navegador con Gerente sobre CloudColorDB; `QA` significa base temporal. Abrir una pantalla no demuestra todos sus botones ni todas las combinaciones. **PENDIENTE** indica que falta completar la verificación indicada; no implica necesariamente un defecto.

| Módulo | Abre | Consultar | Crear/Acción | Editar | Permisos | Resultado |
|---|---|---|---|---|---|---|
| Dashboard | OK real | OK datos y actividad real | OK destinos de tarjetas y actividad real; accesos rápidos abiertos | No aplica | OK menús Gerente real y Cajero/Digitador QA | PENDIENTE revisión integral |
| Clientes, proveedores, productos, empleados | OK real | OK QA | OK QA HTTP | OK QA HTTP | PENDIENTE recorrido manual por rol | PENDIENTE |
| Variantes, bodegas, ubicaciones, proveedor/variante | OK real | OK QA | OK QA | PENDIENTE completar recorrido manual | PENDIENTE navegador por rol | PENDIENTE |
| Cotizaciones | OK real | OK QA | OK QA, confirmar y facturar | PENDIENTE recorrido manual completo | OK QA HTTP | PENDIENTE |
| Facturas y consulta | OK real | CORREGIDO: factura pagada real muestra pagos y lotes | OK QA directa, desde cotización e impresión | No aplica | OK QA HTTP | PENDIENTE terminar recorrido manual |
| Cobros | OK real | OK QA | OK QA HTTP: efectivo, tarjeta, cheque con referencia, cobro exacto y doble cobro rechazado | No aplica | OK QA HTTP | PENDIENTE recorrido manual |
| Devoluciones | OK real | OK real: lote disponible; OK detalle QA | OK QA manual parcial, movimiento de entrada; exceso rechazado por HTTP | No aplica | OK QA HTTP | PENDIENTE revisión integral |
| Anulación | OK diálogo real | OK QA | OK QA manual: motivo, restitución, movimiento y total cero | No aplica | OK QA HTTP | PENDIENTE revisión integral |
| Compras y recepciones | OK real | OK QA | OK QA: 75/25, completa y exceso rechazado | PENDIENTE recorrido manual completo | OK QA HTTP | PENDIENTE |
| Inventario y PEPS | OK real | OK QA: lotes, filtros y PEPS | OK QA en venta y devolución | No aplica | OK QA HTTP | PENDIENTE recorrido manual completo |
| Reportes | OK real | OK QA HTTP | PENDIENTE comprobar todos los filtros y medios manualmente | No aplica | OK QA HTTP | PENDIENTE |
| Usuarios y roles | OK real | OK QA | OK QA HTTP | OK QA HTTP | OK QA HTTP y acceso directo de Cajero/Digitador en navegador | PENDIENTE revisión integral |
| Búsqueda global | OK real | OK real y QA | No aplica | No aplica | OK QA HTTP | PENDIENTE recorrido manual por rol |

Pruebas aisladas ejecutadas: SQL 01–04, C#/WinForms 116 comprobaciones, concurrencia 125 y web HTTP 165. Las suites de navegador verificaron flujos de venta y compra, 45 vistas adaptables, interactividad en 20 vistas y UX en 85 vistas; no detectaron errores de consola. En la sesión del 4 de noviembre también se corrigieron el menú activo al filtrar ventas, el título de acceso denegado y el texto del editor vacío de factura. La base original conservó sus 31 tablas y 119 filas con huella de contenido idéntica tras las suites; no se ejecutó la carga 12. El build Release posterior a los cambios registró 0 errores y 0 advertencias. La suite de concurrencia y la suite web ejecutaron el script 16 en QA; además se ejecutó dos veces consecutivas sin error.

Pendientes de cierre: completar botones, formularios, filtros, permisos y consola durante el recorrido manual; validar los cinco tamaños requeridos en las pantallas pendientes; completar y registrar las suites finales; revisar la documentación y seleccionar evidencias. El ZIP final no debe generarse hasta satisfacer todos los criterios de entrega.
