# Guía de presentación de CloudColor

## Preparación

1. Abrir SSMS y confirmar que la base conectada es `CloudColorDB` en la instancia de demostración autorizada.
2. Iniciar CloudColor Web en `http://localhost:5255` o abrir WinForms como alternativa.
3. Preparar las cuentas de Gerente, Digitador y Cajero en el entorno de presentación. No incluir contraseñas en capturas ni en esta guía.
4. Verificar que los datos de demostración ya están cargados. No volver a ejecutar scripts de creación sobre una base que contiene datos que deban conservarse.

## Recorrido sugerido

1. **Login:** explicar acceso y autenticación protegida con PBKDF2.
2. **Dashboard:** revisar actividad, indicadores y accesos rápidos según rol.
3. **Arquitectura:** presentar Entidades, Datos, Negocio y Presentación; mostrar la cadena hasta SQL Server.
4. **Catálogos:** enseñar Producto y su relación con ProductoVariante, SKU, presentación, color y precio.
5. **Compras:** abrir una orden y explicar proveedor, detalle y progreso recibido/pendiente.
6. **Recepción:** mostrar las recepciones parciales y completas, lotes, costos y ubicaciones.
7. **Inventario:** revisar existencias por bodega, ubicación y lote; explicar orden de salida PEPS.
8. **Cotización:** crear o abrir una cotización, revisar el detalle y confirmar.
9. **Facturación:** mostrar factura desde cotización y factura directa; destacar los precios históricos y el descuento de inventario.
10. **Pagos:** registrar o consultar el pago combinado, indicando medio, monto, referencia y usuario cajero.
11. **Impresión:** abrir “Imprimir factura”; el navegador permite imprimir o “Guardar como PDF”.
12. **Devolución:** mostrar una devolución parcial ligada a la factura y el mismo lote de origen. Explicar que el máximo se calcula restando devoluciones previas.
13. **Anulación:** mostrar el motivo, el estado ANULADA, total actual Q 0.00 y la conservación del total original e historial.
14. **Reportes:** recorrer ventas por fechas, productos más vendidos, mayores ingresos, menos vendidos y variantes sin stock o bajo mínimo.
15. **Consulta de factura:** mostrar cliente, emisor, detalle, pagos/cajero y trazabilidad por lote disponible al Gerente.
16. **Auditoría:** en SSMS consultar las entradas de Bitacora correspondientes a operaciones demostradas, sin exponer contraseñas ni verificadores.
17. **SQL Server:** explicar que el esquema conserva las 31 tablas aprobadas y mostrar SP, función, vistas, TVP, PK/FK, UNIQUE y CHECK.
18. **Pruebas:** resumir las suites SQL, C#, HTTP y navegador; aclarar que QA automatizado corre en bases temporales aisladas.

## Variación por perfil

- **Digitador:** puede demostrar catálogos y flujos operativos asignados (cotización, compra, recepción e inventario); no cobra ni administra usuarios/reportes de Gerente.
- **Cajero:** puede consultar/imprimir facturas y registrar cobros; no emite ventas, administra catálogos, reporta ni anula/devuelve.
- **Gerente:** puede administrar usuarios/roles y supervisar los módulos, reportes, devoluciones y anulaciones.

La UI oculta opciones no autorizadas, pero la aplicación, Negocio y SQL también comprueban el rol. La demostración debe usar sesiones distintas al cambiar de perfil.

## Evidencias visuales

Las capturas se guardan en `docs/final/evidencias/`. Para repetir el recorrido de navegador sin tocar la base principal, usar primero el runner `CloudColor.Web.Pruebas --visual` y luego `npm run test:ux --prefix tests/CloudColor.Web.Browser`. La suite utiliza cuentas y datos ficticios creados únicamente en la base temporal QA.
