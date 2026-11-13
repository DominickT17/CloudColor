USE CloudColorDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF @@TRANCOUNT<>0 THROW 55000,'Ejecute la suite final sin transacciones abiertas.',1;
DECLARE @Paso varchar(100)='preparación',@Diagnostico varchar(200);
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Clave varchar(20)=LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),20);
    DECLARE @E int,@Gerente int,@Digitador int,@Cajero int,@Cliente int,@Proveedor int,@Marca int,@Producto int;
    DECLARE @V1 int,@V2 int,@Bodega int,@Ubicacion int,@Orden int,@LineaOrden int,@Recepcion int,@Lote1 int,@Lote2 int;
    DECLARE @Cotizacion int,@Factura int,@FacturaAnular int,@Devolucion int,@DFL1 int,@DFL2 int,@ID int;
    DECLARE @IDs TABLE(ID int);
    DECLARE @Desde date=CONVERT(date,DATEADD(day,-5,GETDATE())),@Hasta date=CONVERT(date,GETDATE());
    DECLARE @SKU1 varchar(50)=@Clave+'A',@SKU2 varchar(50)=@Clave+'B',@Numero varchar(30),@FechaIngreso datetime;
    INSERT dbo.Empleado(CodigoEmpleado,Nombres,Apellidos,Activo) VALUES(@Clave+'G','Gerente','QA',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario(EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'G','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Gerente'; SET @Gerente=SCOPE_IDENTITY();
    INSERT dbo.Empleado(CodigoEmpleado,Nombres,Apellidos,Activo) VALUES(@Clave+'D','Digitador','QA',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario(EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'D','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Digitador'; SET @Digitador=SCOPE_IDENTITY();
    INSERT dbo.Empleado(CodigoEmpleado,Nombres,Apellidos,Activo) VALUES(@Clave+'C','Cajero','QA',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario(EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'C','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Cajero'; SET @Cajero=SCOPE_IDENTITY();
    INSERT @IDs EXEC dbo.sp_ClienteInsertar @Digitador,'Particular','Cliente reporte QA'; SELECT @Cliente=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_ProveedorInsertar @Digitador,'Proveedor reporte QA'; SELECT @Proveedor=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_MarcaInsertar @Digitador,@Clave; SELECT @Marca=ID FROM @IDs; DELETE @IDs;
    DECLARE @Categoria int=(SELECT CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre='Pinturas');
    DECLARE @Presentacion int=(SELECT PresentacionID FROM dbo.Presentacion WHERE Nombre='1 galon');
    INSERT @IDs EXEC dbo.sp_ProductoInsertar @Digitador,@Categoria,@Marca,'Producto reporte QA',NULL,1,1; SELECT @Producto=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_ProductoVarianteGuardar @Digitador,@Producto,@Presentacion,NULL,@SKU1,10,2; SELECT @V1=ID FROM @IDs; DELETE @IDs;
    SET @Presentacion=(SELECT PresentacionID FROM dbo.Presentacion WHERE Nombre='1 cubeta');
    INSERT @IDs EXEC dbo.sp_ProductoVarianteGuardar @Digitador,@Producto,@Presentacion,NULL,@SKU2,20,1; SELECT @V2=ID FROM @IDs; DELETE @IDs;
    EXEC dbo.sp_ProveedorProductoGuardar @Digitador,@Proveedor,@V1;
    INSERT @IDs EXEC dbo.sp_BodegaGuardar @Digitador,@Clave,'Bodega reporte QA'; SELECT @Bodega=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_UbicacionGuardar @Digitador,@Bodega,'A-01'; SELECT @Ubicacion=ID FROM @IDs; DELETE @IDs;
    DECLARE @Compra dbo.TipoDetalleOrdenCompra,@Recibo dbo.TipoDetalleRecepcion;
    INSERT @Compra VALUES(@V1,15,5);
    INSERT @IDs EXEC dbo.sp_OrdenCompraCrear @Digitador,@Clave,@Proveedor,@Compra; SELECT @Orden=ID FROM @IDs; DELETE @IDs;
    SELECT @LineaOrden=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden;
    INSERT @Recibo VALUES(@LineaOrden,5,5,@Ubicacion,'QA-LOTE-1',NULL);
    SET @Numero=@Clave+'R1'; SET @FechaIngreso=DATEADD(day,-2,GETDATE());
    INSERT @IDs EXEC dbo.sp_RecepcionCompraRegistrar @Digitador,@Numero,@Orden,@FechaIngreso,@Recibo; SELECT @Recepcion=ID FROM @IDs; DELETE @IDs; DELETE @Recibo;
    SELECT @Lote1=l.LoteInventarioID FROM dbo.LoteInventario l JOIN dbo.DetalleRecepcionCompra r ON r.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID WHERE r.RecepcionCompraID=@Recepcion;
    INSERT @Recibo VALUES(@LineaOrden,10,5,@Ubicacion,'QA-LOTE-2',NULL);
    SET @Numero=@Clave+'R2'; SET @FechaIngreso=DATEADD(day,-1,GETDATE());
    INSERT @IDs EXEC dbo.sp_RecepcionCompraRegistrar @Digitador,@Numero,@Orden,@FechaIngreso,@Recibo; SELECT @Recepcion=ID FROM @IDs; DELETE @IDs; DELETE @Recibo;
    SELECT @Lote2=l.LoteInventarioID FROM dbo.LoteInventario l JOIN dbo.DetalleRecepcionCompra r ON r.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID WHERE r.RecepcionCompraID=@Recepcion;

    DECLARE @Venta dbo.TipoDetalleFactura,@Vacia dbo.TipoDetalleFactura;
    INSERT @Venta VALUES(@V1,8,10);
    SET @Numero=@Clave+'F1';
    INSERT @IDs EXEC dbo.sp_FacturaGenerar @Digitador,@Numero,@Cliente,@Venta; SELECT @Factura=ID FROM @IDs; DELETE @IDs;
    SELECT @DFL1=fl.DetalleFacturaLoteID FROM dbo.DetalleFacturaLote fl JOIN dbo.LoteInventario l ON l.LoteInventarioID=fl.LoteInventarioID WHERE fl.DetalleFacturaID=(SELECT DetalleFacturaID FROM dbo.DetalleFactura WHERE FacturaID=@Factura) AND l.LoteInventarioID=@Lote1;
    SELECT @DFL2=fl.DetalleFacturaLoteID FROM dbo.DetalleFacturaLote fl JOIN dbo.LoteInventario l ON l.LoteInventarioID=fl.LoteInventarioID WHERE fl.DetalleFacturaID=(SELECT DetalleFacturaID FROM dbo.DetalleFactura WHERE FacturaID=@Factura) AND l.LoteInventarioID=@Lote2;
    IF @DFL1 IS NULL OR @DFL2 IS NULL THROW 55001,'La factura de prueba no usa dos lotes PEPS.',1;
    DECLARE @Disponibles TABLE
    (
        DetalleFacturaLoteID int,DetalleFacturaID int,ProductoVarianteID int,CodigoSKU varchar(50),
        Producto varchar(180),Presentacion varchar(100),Color varchar(80),PrecioUnitario decimal(18,2),
        CantidadSalida decimal(18,4),CantidadDevuelta decimal(18,4),CantidadDisponible decimal(18,4),
        NumeroLoteProveedor varchar(60),FechaIngreso datetime,Ubicacion varchar(80),Bodega varchar(120)
    );
    SET @Paso='consultar devoluciones disponibles';
    INSERT @Disponibles EXEC dbo.sp_DevolucionDisponibles @Gerente,@Factura;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE UsuarioID=@Gerente) THROW 55019,'La cuenta Gerente no existe tras consultar devoluciones.',1;
    IF (SELECT SUM(CantidadDisponible) FROM @Disponibles)<>8 THROW 55002,'Las unidades disponibles para devolución no coinciden con la venta.',1;
    DECLARE @DetalleDev dbo.TipoDetalleDevolucion,@Rechazo bit=0;
    INSERT @DetalleDev VALUES(@DFL1,2);
    SET @Numero=@Clave+'DV1';
    SET @Paso='primera devolución';
    INSERT @IDs EXEC dbo.sp_DevolucionCrear @Gerente,@Numero,@Factura,'Devolución parcial QA',@DetalleDev; SELECT @Devolucion=ID FROM @IDs; DELETE @IDs; DELETE @DetalleDev;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE UsuarioID=@Gerente) THROW 55020,'La cuenta Gerente no existe tras la primera devolución.',1;
    IF (SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@Lote1)<>2 OR NOT EXISTS(SELECT 1 FROM dbo.MovimientoInventario WHERE Referencia=@Numero AND TipoMovimiento='ENTRADA_DEVOLUCION' AND Cantidad=2)
        THROW 55003,'La devolución no restituyó al mismo lote ni registró movimiento.',1;
    INSERT @DetalleDev VALUES(@DFL2,2);
    SET @Numero=@Clave+'DV2';
    SET @Paso='segunda devolución';
    INSERT @IDs EXEC dbo.sp_DevolucionCrear @Gerente,@Numero,@Factura,'Segunda devolución QA',@DetalleDev; DELETE @IDs; DELETE @DetalleDev;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE UsuarioID=@Gerente) THROW 55021,'La cuenta Gerente no existe tras la segunda devolución.',1;
    INSERT @DetalleDev VALUES(@DFL1,4); SET @Rechazo=0; SET @Numero=@Clave+'DV3';
    BEGIN TRY EXEC dbo.sp_DevolucionCrear @Gerente,@Numero,@Factura,'Exceso QA',@DetalleDev; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52121 THROW; SET @Rechazo=1; END CATCH;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE UsuarioID=@Gerente) THROW 55022,'La cuenta Gerente no existe tras el intento de exceso.',1;
    IF @Rechazo=0 OR EXISTS(SELECT 1 FROM dbo.Devolucion WHERE NumeroDevolucion=@Numero) THROW 55004,'Se permitió devolver más de lo que quedaba del lote.',1;
    DECLARE @CantidadNeta decimal(18,4),@IngresoNeto decimal(18,2);
    SELECT @CantidadNeta=CantidadNeta,@IngresoNeto=IngresoNeto FROM dbo.vw_VentasDetalleNeto WHERE FacturaID=@Factura;
    IF @CantidadNeta<>4 OR @IngresoNeto<>40
    BEGIN
        SET @Diagnostico=CONCAT('Resultado neto inesperado: cantidad=',COALESCE(CONVERT(varchar(30),@CantidadNeta),'NULL'),'; ingreso=',COALESCE(CONVERT(varchar(30),@IngresoNeto),'NULL'));
        THROW 55005,@Diagnostico,1;
    END;
    DECLARE @Reporte TABLE(TotalFacturado decimal(18,2),TotalEfectivo decimal(18,2),TotalCheque decimal(18,2),TotalTarjeta decimal(18,2),CantidadFacturas int);
    SET @Paso='reporte de ventas';
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID=u.RolID JOIN dbo.Empleado e ON e.EmpleadoID=u.EmpleadoID WHERE u.UsuarioID=@Gerente AND r.Nombre='Gerente' AND u.Activo=1 AND r.Activo=1 AND e.Activo=1)
    BEGIN
        SET @Diagnostico=CONCAT('Cuenta gerente invalida id=',@Gerente,'; rol=',COALESCE((SELECT r.Nombre FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID=u.RolID WHERE u.UsuarioID=@Gerente),'NULL'),'; usuario activo=',COALESCE((SELECT CONVERT(varchar(5),Activo) FROM dbo.Usuario WHERE UsuarioID=@Gerente),'NULL'));
        THROW 55018,@Diagnostico,1;
    END;
    INSERT @Reporte EXEC dbo.sp_ReporteVentas @Gerente,@Desde,@Hasta;
    IF NOT EXISTS(SELECT 1 FROM @Reporte WHERE TotalFacturado=40 AND CantidadFacturas=1) THROW 55006,'El reporte por fechas no suma unidades devueltas correctamente.',1;
    SET @Rechazo=0;
    SET @Paso='reporte con Digitador';
    BEGIN TRY EXEC dbo.sp_ReporteVentas @Digitador,@Hasta,@Hasta; END TRY
    BEGIN CATCH
        IF ERROR_NUMBER()<>52001
        BEGIN
            SET @Diagnostico=CONCAT('Autorización de Digitador: error ',ERROR_NUMBER(),'; ',ERROR_MESSAGE());
            THROW 55017,@Diagnostico,1;
        END;
        SET @Rechazo=1;
    END CATCH;
    IF @Rechazo=0 THROW 55007,'Digitador pudo consultar reporte exclusivo de Gerente.',1;
    DECLARE @Mas TABLE(ProductoVarianteID int,Producto varchar(180),CodigoSKU varchar(50),Presentacion varchar(100),Color varchar(80),CantidadVendida decimal(18,4),Ingresos decimal(18,2));
    SET @Paso='reporte más vendidos';
    INSERT @Mas EXEC dbo.sp_ReporteProductos @Gerente,@Desde,@Hasta,'MAS_VENDIDOS',5;
    IF NOT EXISTS(SELECT 1 FROM @Mas WHERE ProductoVarianteID=@V1 AND CantidadVendida=4 AND Ingresos=40) THROW 55008,'Más vendidos no usa cantidad neta.',1;
    DELETE @Mas;
    INSERT @Mas EXEC dbo.sp_ReporteProductos @Gerente,@Desde,@Hasta,'MAYORES_INGRESOS',10;
    IF NOT EXISTS(SELECT 1 FROM @Mas WHERE ProductoVarianteID=@V1 AND Ingresos=40) THROW 55009,'Mayores ingresos no usa precio histórico neto.',1;
    DELETE @Mas;
    INSERT @Mas EXEC dbo.sp_ReporteProductos @Gerente,@Desde,@Hasta,'MENOS_VENDIDOS',0;
    IF NOT EXISTS(SELECT 1 FROM @Mas WHERE ProductoVarianteID=@V2 AND CantidadVendida=0) THROW 55010,'Menos vendidos no incluye variante sin ventas.',1;
    DECLARE @Stock TABLE(ProductoVarianteID int,Producto varchar(180),CodigoSKU varchar(50),Presentacion varchar(100),Color varchar(80),Disponible decimal(18,4),StockMinimo decimal(18,4),Estado varchar(20),Proveedor varchar(180));
    SET @Paso='reporte sin stock';
    INSERT @Stock EXEC dbo.sp_ReporteSinStock @Gerente;
    IF NOT EXISTS(SELECT 1 FROM @Stock WHERE ProductoVarianteID=@V2 AND Disponible=0 AND Proveedor IS NULL) THROW 55011,'El reporte sin stock no identifica variante agotada.',1;

    DECLARE @MedioEfectivo int=(SELECT MedioPagoID FROM dbo.MedioPago WHERE Nombre='Efectivo'),@MedioTarjeta int=(SELECT MedioPagoID FROM dbo.MedioPago WHERE Nombre='Tarjeta');
    DECLARE @Pagos dbo.TipoPagoFactura;
    SET @Paso='cobro de prueba';
    INSERT @Pagos VALUES(@MedioEfectivo,50,NULL),(@MedioTarjeta,30,'QA-TARJETA');
    EXEC dbo.sp_FacturaCobrar @Cajero,@Factura,@Pagos;
    DELETE @Reporte;
    INSERT @Reporte EXEC dbo.sp_ReporteVentas @Gerente,@Desde,@Hasta;
    IF NOT EXISTS(SELECT 1 FROM @Reporte WHERE TotalEfectivo=50 AND TotalTarjeta=30 AND TotalCheque=0) THROW 55012,'El reporte no agrupa medios de pago.',1;

    DELETE @Venta; INSERT @Venta VALUES(@V1,2,10); SET @Numero=@Clave+'F2';
    SET @Paso='factura a anular';
    INSERT @IDs EXEC dbo.sp_FacturaGenerar @Digitador,@Numero,@Cliente,@Venta; SELECT @FacturaAnular=ID FROM @IDs; DELETE @IDs;
    DECLARE @Antes decimal(18,4)=(SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@Lote1);
    SET @Paso='anulación';
    EXEC dbo.sp_FacturaAnular @Gerente,@FacturaAnular,'Anulación de prueba';
    IF NOT EXISTS(SELECT 1 FROM dbo.Factura WHERE FacturaID=@FacturaAnular AND Estado='ANULADA' AND TotalOriginal=20 AND TotalActual=0)
        THROW 55013,'La anulación no conserva original y pone total actual en cero.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.AnulacionFactura WHERE FacturaID=@FacturaAnular AND Motivo='Anulación de prueba')
       OR (SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@Lote1)<>@Antes+2
       OR NOT EXISTS(SELECT 1 FROM dbo.MovimientoInventario WHERE Referencia=@Numero AND TipoMovimiento='ENTRADA_ANULACION' AND Cantidad=2)
        THROW 55014,'La anulación no conservó motivo, devolvió al lote original o registró movimiento.',1;
    SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaAnular @Gerente,@FacturaAnular,'Repetición'; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 THROW 55015,'La factura se anuló dos veces.',1;
    DELETE @Reporte;
    INSERT @Reporte EXEC dbo.sp_ReporteVentas @Gerente,@Desde,@Hasta;
    IF NOT EXISTS(SELECT 1 FROM @Reporte WHERE CantidadFacturas=1) THROW 55016,'El reporte cuenta una factura anulada.',1;
    PRINT 'OK: reportes de ventas y producto, permisos, sin stock, devolución parcial/exceso, trazabilidad y anulación transaccional.';
    ROLLBACK TRANSACTION;
    PRINT 'OK Proyecto Final: todas las comprobaciones revertidas.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    SET @Diagnostico=CONCAT(@Paso,': ',ERROR_MESSAGE());
    THROW 55999,@Diagnostico,1;
END CATCH;
GO
