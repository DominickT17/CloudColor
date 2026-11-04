# Checklist de entrega final

Revisión del 4 de noviembre de 2026, en progreso. `Real` significa consulta en navegador con Gerente sobre CloudColorDB; `QA` significa base temporal. Abrir una pantalla no demuestra todos sus botones ni todas las combinaciones. **PENDIENTE** indica que falta completar la verificación indicada; no implica necesariamente un defecto.

| Módulo | Abre | Consultar | Crear/Acción | Editar | Permisos | Resultado |
|---|---|---|---|---|---|---|
| Dashboard | OK real | OK datos y actividad real | OK destinos de tarjetas y actividad real; accesos rápidos abiertos | No aplica | PENDIENTE navegador por rol | PENDIENTE |
| Clientes, proveedores, productos, empleados | OK real | OK QA | OK QA HTTP | OK QA HTTP | PENDIENTE recorrido manual por rol | PENDIENTE |
| Variantes, bodegas, ubicaciones, proveedor/variante | OK real | OK QA | OK QA | PENDIENTE completar recorrido manual | PENDIENTE navegador por rol | PENDIENTE |
| Cotizaciones | OK real | OK QA | OK QA, confirmar y facturar | PENDIENTE recorrido manual completo | OK QA HTTP | PENDIENTE |
| Facturas y consulta | OK real | CORREGIDO: factura pagada real muestra pagos y lotes | OK QA directa, desde cotización e impresión | No aplica | OK QA HTTP | PENDIENTE terminar recorrido manual |
| Cobros | OK real | OK QA | OK QA múltiple y doble cobro rechazado | No aplica | OK QA HTTP | PENDIENTE revisar cheque y recorrido manual |
| Devoluciones | OK real | OK QA | OK QA parcial, lote y exceso rechazado | No aplica | OK QA HTTP | PENDIENTE recorrido manual completo |
| Anulación | OK QA | OK QA | OK QA: motivo, restitución y total cero | No aplica | OK QA HTTP | PENDIENTE recorrido manual completo |
| Compras y recepciones | OK real | OK QA | OK QA: 75/25, completa y exceso rechazado | PENDIENTE recorrido manual completo | OK QA HTTP | PENDIENTE |
| Inventario y PEPS | OK real | OK QA: lotes, filtros y PEPS | OK QA en venta y devolución | No aplica | OK QA HTTP | PENDIENTE recorrido manual completo |
| Reportes | OK real | OK QA HTTP | PENDIENTE comprobar todos los filtros y medios manualmente | No aplica | OK QA HTTP | PENDIENTE |
| Usuarios y roles | OK real | OK QA | OK QA HTTP | OK QA HTTP | OK QA HTTP | PENDIENTE recorrido manual por rol |
| Búsqueda global | OK real | OK real y QA | No aplica | No aplica | OK QA HTTP | PENDIENTE recorrido manual por rol |

Pruebas aisladas ejecutadas: SQL 01–04, C#/WinForms 116 comprobaciones, concurrencia 125 y web HTTP 164. Las suites de navegador verificaron flujos de venta y compra, 45 vistas adaptables, interactividad en 20 vistas y UX en 85 vistas; no detectaron errores de consola. La base original conservó sus 31 tablas y 119 filas con huella de contenido idéntica tras las suites; no se ejecutó la carga 12. El build Release posterior a los cambios registró 0 errores y 0 advertencias. La suite de concurrencia y la suite web ejecutaron el script 16 en QA; además se ejecutó dos veces consecutivas sin error.

Pendientes de cierre: completar botones, formularios, filtros, permisos y consola durante el recorrido manual; validar los cinco tamaños requeridos en las pantallas pendientes; completar y registrar las suites finales; revisar la documentación y seleccionar evidencias. El ZIP final no debe generarse hasta satisfacer todos los criterios de entrega.
