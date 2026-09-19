-- Ejecutar despues de 00, 01 y 02. No deja registros de prueba.
-- El ROLLBACK no revierte los numeros IDENTITY consumidos: puede haber saltos.
-- Ejecutar sin una transaccion previa y sin escrituras concurrentes.
USE CloudColorDB;
GO
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT OFF; -- Permite comprobar errores esperados sin abortar toda la prueba.
IF @@TRANCOUNT <> 0
    THROW 51000, 'Ejecute las pruebas sin una transaccion abierta.', 1;
BEGIN TRY
    IF OBJECT_ID('dbo.Rol', 'U') IS NULL THROW 51001, 'Falta la tabla Rol.', 1;
    IF OBJECT_ID('dbo.Empleado', 'U') IS NULL THROW 51001, 'Falta la tabla Empleado.', 1;
    IF OBJECT_ID('dbo.Usuario', 'U') IS NULL THROW 51001, 'Falta la tabla Usuario.', 1;
    IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL THROW 51001, 'Falta la tabla Bitacora.', 1;
    IF OBJECT_ID('dbo.CategoriaProducto', 'U') IS NULL THROW 51001, 'Falta la tabla CategoriaProducto.', 1;
    IF OBJECT_ID('dbo.Marca', 'U') IS NULL THROW 51001, 'Falta la tabla Marca.', 1;
    IF OBJECT_ID('dbo.Presentacion', 'U') IS NULL THROW 51001, 'Falta la tabla Presentacion.', 1;
    IF OBJECT_ID('dbo.Color', 'U') IS NULL THROW 51001, 'Falta la tabla Color.', 1;
    IF OBJECT_ID('dbo.Producto', 'U') IS NULL THROW 51001, 'Falta la tabla Producto.', 1;
    IF OBJECT_ID('dbo.ProductoVariante', 'U') IS NULL THROW 51001, 'Falta la tabla ProductoVariante.', 1;
    IF OBJECT_ID('dbo.Cliente', 'U') IS NULL THROW 51001, 'Falta la tabla Cliente.', 1;
    IF OBJECT_ID('dbo.Proveedor', 'U') IS NULL THROW 51001, 'Falta la tabla Proveedor.', 1;
    IF OBJECT_ID('dbo.ProveedorProducto', 'U') IS NULL THROW 51001, 'Falta la tabla ProveedorProducto.', 1;
    IF OBJECT_ID('dbo.Bodega', 'U') IS NULL THROW 51001, 'Falta la tabla Bodega.', 1;
    IF OBJECT_ID('dbo.Ubicacion', 'U') IS NULL THROW 51001, 'Falta la tabla Ubicacion.', 1;
    IF OBJECT_ID('dbo.OrdenCompra', 'U') IS NULL THROW 51001, 'Falta la tabla OrdenCompra.', 1;
    IF OBJECT_ID('dbo.DetalleOrdenCompra', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleOrdenCompra.', 1;
    IF OBJECT_ID('dbo.RecepcionCompra', 'U') IS NULL THROW 51001, 'Falta la tabla RecepcionCompra.', 1;
    IF OBJECT_ID('dbo.DetalleRecepcionCompra', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleRecepcionCompra.', 1;
    IF OBJECT_ID('dbo.LoteInventario', 'U') IS NULL THROW 51001, 'Falta la tabla LoteInventario.', 1;
    IF OBJECT_ID('dbo.MovimientoInventario', 'U') IS NULL THROW 51001, 'Falta la tabla MovimientoInventario.', 1;
    IF OBJECT_ID('dbo.Cotizacion', 'U') IS NULL THROW 51001, 'Falta la tabla Cotizacion.', 1;
    IF OBJECT_ID('dbo.DetalleCotizacion', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleCotizacion.', 1;
    IF OBJECT_ID('dbo.Factura', 'U') IS NULL THROW 51001, 'Falta la tabla Factura.', 1;
    IF OBJECT_ID('dbo.DetalleFactura', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleFactura.', 1;
    IF OBJECT_ID('dbo.DetalleFacturaLote', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleFacturaLote.', 1;
    IF OBJECT_ID('dbo.MedioPago', 'U') IS NULL THROW 51001, 'Falta la tabla MedioPago.', 1;
    IF OBJECT_ID('dbo.PagoFactura', 'U') IS NULL THROW 51001, 'Falta la tabla PagoFactura.', 1;
    IF OBJECT_ID('dbo.Devolucion', 'U') IS NULL THROW 51001, 'Falta la tabla Devolucion.', 1;
    IF OBJECT_ID('dbo.DetalleDevolucion', 'U') IS NULL THROW 51001, 'Falta la tabla DetalleDevolucion.', 1;
    IF OBJECT_ID('dbo.AnulacionFactura', 'U') IS NULL THROW 51001, 'Falta la tabla AnulacionFactura.', 1;
    IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) <> 31
        THROW 51002, 'Se esperaban exactamente 31 tablas.', 1;
    IF (SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK') <> 31
        THROW 51003, 'Se esperaban 31 PK.', 1;
    IF (SELECT COUNT(*) FROM sys.foreign_keys) <> 43
        THROW 51004, 'Se esperaban 43 FK.', 1;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled = 1 OR is_not_trusted = 1 OR delete_referential_action <> 0)
        THROW 51005, 'FK deshabilitada, no confiable o con accion de borrado.', 1;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_disabled = 1 OR is_not_trusted = 1)
        THROW 51006, 'CHECK deshabilitado o no confiable.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Factura')
                   AND name = 'UX_Factura_CotizacionID_NotNull' AND is_unique = 1 AND has_filter = 1)
        THROW 51007, 'Falta el indice unico filtrado de cotizacion.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Digitador' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Rol / Digitador.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Cajero' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Rol / Cajero.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Gerente' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Rol / Gerente.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Accesorios' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Accesorios.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Solventes' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Solventes.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Pinturas' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Pinturas.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Barnices' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Barnices.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Lacas' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Lacas.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Selladores' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: CategoriaProducto / Selladores.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Efectivo' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: MedioPago / Efectivo.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Cheque' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: MedioPago / Cheque.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Tarjeta' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: MedioPago / Tarjeta.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = 'Unidad' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / Unidad.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/32 de galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1/32 de galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/16 de galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1/16 de galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/8 de galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1/8 de galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/4 de galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1/4 de galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/2 de galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1/2 de galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1 galon' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1 galon.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1 cubeta' AND Activo = 1)
        THROW 51008, 'Falta catalogo inicial: Presentacion / 1 cubeta.', 1;
    PRINT 'OK: tablas, claves, indice y catalogos.';
    BEGIN TRANSACTION;
    DECLARE @Sufijo varchar(36) = CONVERT(varchar(36), NEWID());
    DECLARE @Rol1 int, @Rol2 int, @Empleado int, @Usuario int, @Cliente int;
    DECLARE @Cotizacion int, @Factura int, @Medio int, @Rechazado bit;
    INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('T1-' + @Sufijo, 1);
    SET @Rol1 = SCOPE_IDENTITY();
    INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('T2-' + @Sufijo, 1);
    SET @Rol2 = SCOPE_IDENTITY();
    SET IDENTITY_INSERT dbo.Rol ON;
    SET @Rechazado = 0;
    BEGIN TRY
        INSERT INTO dbo.Rol (RolID, Nombre, Activo) VALUES (@Rol1, 'T3-' + @Sufijo, 1);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2627) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: PK rechaza identificador duplicado.', 1;
    PRINT 'OK: PK rechaza identificador duplicado.';
    SET IDENTITY_INSERT dbo.Rol OFF;
    SET @Rechazado = 0;
    BEGIN TRY
        INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('T1-' + @Sufijo, 1);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2627, 2601) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: UNIQUE rechaza nombre duplicado.', 1;
    PRINT 'OK: UNIQUE rechaza nombre duplicado.';
    SET @Rechazado = 0;
    BEGIN TRY
        INSERT INTO dbo.Ubicacion (BodegaID, Codigo, Activo) VALUES (-2147483648, 'TEST', 1);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (547) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: FK rechaza bodega inexistente.', 1;
    PRINT 'OK: FK rechaza bodega inexistente.';
    INSERT INTO dbo.Empleado (CodigoEmpleado, Nombres, Apellidos, Activo)
        VALUES ('T' + LEFT(@Sufijo, 25), 'PRUEBA TEMPORAL', 'ROLLBACK', 1);
    SET @Empleado = SCOPE_IDENTITY();
    INSERT INTO dbo.Usuario (EmpleadoID, RolID, NombreUsuario, PasswordHash, Activo)
        VALUES (@Empleado, @Rol1, 'T-' + @Sufijo, 'NO_ES_CREDENCIAL_VALIDA_SOLO_PRUEBA', 1);
    SET @Usuario = SCOPE_IDENTITY();
    INSERT INTO dbo.Cliente (TipoCliente, NombreRazonSocial, Activo)
        VALUES ('Particular', 'PRUEBA TEMPORAL ROLLBACK', 1);
    SET @Cliente = SCOPE_IDENTITY();
    INSERT INTO dbo.Cotizacion (NumeroCotizacion, ClienteID, UsuarioID, Fecha, Estado)
        VALUES ('T' + LEFT(@Sufijo, 25), @Cliente, @Usuario, GETDATE(), 'PRUEBA');
    SET @Cotizacion = SCOPE_IDENTITY();
    INSERT INTO dbo.Factura (NumeroFactura, ClienteID, CotizacionID, UsuarioEmisorID, Fecha, Estado, TotalOriginal, TotalActual)
        VALUES ('T1' + LEFT(@Sufijo, 25), @Cliente, @Cotizacion, @Usuario, GETDATE(), 'PRUEBA', 10, 10);
    SET @Factura = SCOPE_IDENTITY();
    SET @Rechazado = 0;
    BEGIN TRY
        INSERT INTO dbo.Factura (NumeroFactura, ClienteID, CotizacionID, UsuarioEmisorID, Fecha, Estado, TotalOriginal, TotalActual)
        VALUES ('T2' + LEFT(@Sufijo, 25), @Cliente, @Cotizacion, @Usuario, GETDATE(), 'PRUEBA', 10, 10);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: indice filtrado rechaza cotizacion repetida.', 1;
    PRINT 'OK: indice filtrado rechaza cotizacion repetida.';
    INSERT INTO dbo.Factura (NumeroFactura, ClienteID, CotizacionID, UsuarioEmisorID, Fecha, Estado, TotalOriginal, TotalActual)
        VALUES ('T3' + LEFT(@Sufijo, 25), @Cliente, NULL, @Usuario, GETDATE(), 'PRUEBA', 10, 10),
               ('T4' + LEFT(@Sufijo, 25), @Cliente, NULL, @Usuario, GETDATE(), 'PRUEBA', 10, 10);
    IF (SELECT COUNT(*) FROM dbo.Factura WHERE ClienteID = @Cliente AND CotizacionID IS NULL) <> 2
        THROW 51011, 'No se permitieron dos facturas sin cotizacion.', 1;
    PRINT 'OK: multiples facturas con cotizacion NULL.';
    SET @Rechazado = 0;
    BEGIN TRY
        UPDATE dbo.Factura SET TotalActual = -1 WHERE FacturaID = @Factura;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (547) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: CHECK rechaza total negativo.', 1;
    PRINT 'OK: CHECK rechaza total negativo.';
    SET @Rechazado = 0;
    BEGIN TRY
        UPDATE dbo.Factura SET TotalActual = 11 WHERE FacturaID = @Factura;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (547) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: CHECK rechaza total superior al original.', 1;
    PRINT 'OK: CHECK rechaza total superior al original.';
    SELECT @Medio = MedioPagoID FROM dbo.MedioPago WHERE Nombre = 'Efectivo';
    SET @Rechazado = 0;
    BEGIN TRY
        INSERT INTO dbo.PagoFactura (FacturaID, MedioPagoID, UsuarioCajeroID, FechaHora, Monto)
        VALUES (@Factura, @Medio, @Usuario, GETDATE(), 0);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (547) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: CHECK rechaza pago de cero.', 1;
    PRINT 'OK: CHECK rechaza pago de cero.';
    SET @Rechazado = 0;
    BEGIN TRY
        DELETE FROM dbo.Cliente WHERE ClienteID = @Cliente;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (547) THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 51010, 'Fallo: FK protege historial al borrar cliente.', 1;
    PRINT 'OK: FK protege historial al borrar cliente.';
    ROLLBACK TRANSACTION;
    SET XACT_ABORT ON;
    PRINT 'OK: todas las pruebas terminaron; registros temporales revertidos.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET IDENTITY_INSERT dbo.Rol OFF;
    SET XACT_ABORT ON;
    THROW;
END CATCH;
GO
