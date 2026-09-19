# CloudColor

Proyecto Final de Bases de Datos II para administrar una empresa de pinturas y productos relacionados. La base de datos concentra el catálogo, compras con entregas parciales, inventario por ubicación y lote, cotizaciones, facturas, pagos, devoluciones y auditoría.

## Tecnología

- Microsoft SQL Server 2022 Express y T-SQL.
- SQL Server Management Studio (SSMS) para ejecutar los scripts.
- DrawDB / DBML para el modelo relacional.
- Aplicación de escritorio multicapa en C#/.NET, pendiente.

## Archivos

- `docs/enunciado/Enunciado_Proyecto_Final_2026.pdf`: enunciado oficial.
- `database/model/CloudColor_Modelo_Relacional.dbml`: modelo aprobado, conservado sin cambios.
- `database/scripts/00_CreateDatabase.sql`: creación de CloudColorDB.
- `database/scripts/01_CreateTables.sql`: 31 tablas y restricciones.
- `database/scripts/02_InitialData.sql`: catálogos mínimos.
- `database/tests/01_TestStructure.sql`: verificaciones de estructura e integridad.
- `src/`: reservado para la aplicación; todavía sin implementación.

## Ejecución en SSMS

Conectarse a SQL Server 2022 Express, por ejemplo `.\SQLEXPRESS`, con permisos de creación de base y objetos. Abrir y ejecutar completos, en este orden:

1. `database/scripts/00_CreateDatabase.sql`
2. `database/scripts/01_CreateTables.sql`
3. `database/scripts/02_InitialData.sql`
4. `database/tests/01_TestStructure.sql`

Detenerse si un archivo devuelve un error. Los scripts seleccionan explícitamente `CloudColorDB`. El primero conserva una base existente. El segundo requiere una base sin tablas de usuario y rechaza su repetición sin reemplazar objetos; crea toda la estructura en una transacción. El tercero puede repetirse: solo inserta nombres ausentes, sin sobrescribir registros existentes.

El cuarto debe ejecutarse sin transacciones abiertas ni escrituras concurrentes. Utiliza datos sintéticos solo para comprobar restricciones y siempre los revierte. No deja clientes, empleados, usuarios ni facturas de prueba. Los valores IDENTITY consumidos no se recuperan con ROLLBACK, por lo que pueden aparecer saltos normales en los identificadores.

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

## Estado y Fase I pendiente

Estructura inicial ejecutada y verificada en SQL Server 2022 Express local. Las pruebas comprueban tablas, claves, catálogos, rechazo de duplicados, FK inválidas, CHECK, protección de registros referenciados y ambos casos del índice filtrado. No se han creado procedimientos, funciones, vistas, triggers ni aplicación.

Para completar Fase I faltan:

- Documentar la justificación de 3FN y el diseño relacional.
- Definir e implementar la protección y validación de contraseñas desde la base de datos conforme al enunciado; PasswordHash por sí solo no implementa autenticación.
- Administración de Usuario y Rol.
- Cuatro mantenimientos principales: **Cliente, Proveedor, Producto y Empleado**. Marca y Presentacion son catálogos auxiliares.
- Procedimientos parametrizados y las funciones/vistas pertinentes a los criterios del curso, cuando se autorice esa etapa.
- Aplicación de escritorio multicapa y pruebas de sus pantallas.

Las tablas también preparan Fase II (compras, recepciones, cotización, facturación transaccional, pagos, impresión y PEPS) y Fase Final (reportes y anulaciones), pero esos procesos aún no están implementados.
