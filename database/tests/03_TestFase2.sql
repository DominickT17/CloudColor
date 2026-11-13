USE CloudColorDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF @@TRANCOUNT<>0 THROW 54000, 'Ejecute sin transacciones abiertas.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Clave varchar(20)=LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),20);
    DECLARE @G int,@D int,@C int,@E int,@Cliente int,@Proveedor int,@Marca int,@Producto int,@V1 int,@V2 int,@B int,@U int,@Orden int,@R1 int,@R2 int,@L1 int,@L2 int,@O1 int,@O2 int,@Cot int,@Factura int,@Directa int;
    DECLARE @IDs TABLE (ID int);
    INSERT dbo.Empleado (CodigoEmpleado,Nombres,Apellidos,Activo) VALUES (@Clave+'G','Gerente','Temporal',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario (EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'G','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Gerente'; SET @G=SCOPE_IDENTITY();
    INSERT dbo.Empleado (CodigoEmpleado,Nombres,Apellidos,Activo) VALUES (@Clave+'D','Digitador','Temporal',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario (EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'D','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Digitador'; SET @D=SCOPE_IDENTITY();
    INSERT dbo.Empleado (CodigoEmpleado,Nombres,Apellidos,Activo) VALUES (@Clave+'C','Cajero','Temporal',1); SET @E=SCOPE_IDENTITY();
    INSERT dbo.Usuario (EmpleadoID,RolID,NombreUsuario,PasswordHash,Activo) SELECT @E,RolID,@Clave+'C','NO_LOGIN_PRUEBA',1 FROM dbo.Rol WHERE Nombre='Cajero'; SET @C=SCOPE_IDENTITY();
    INSERT @IDs EXEC dbo.sp_ClienteInsertar @D,'Particular','Cliente prueba Fase II'; SELECT @Cliente=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_ProveedorInsertar @D,'Proveedor prueba Fase II'; SELECT @Proveedor=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_MarcaInsertar @D,@Clave; SELECT @Marca=ID FROM @IDs; DELETE @IDs;
    DECLARE @Categoria int=(SELECT CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre='Pinturas'),@Presentacion int=(SELECT PresentacionID FROM dbo.Presentacion WHERE Nombre='1 galon');
    INSERT @IDs EXEC dbo.sp_ProductoInsertar @D,@Categoria,@Marca,'Producto prueba Fase II',NULL,1,1; SELECT @Producto=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_ProductoVarianteGuardar @D,@Producto,@Presentacion,NULL,@Clave,10,2; SELECT @V1=ID FROM @IDs; DELETE @IDs;
    SELECT @Presentacion=PresentacionID FROM dbo.Presentacion WHERE Nombre='1 cubeta';
    DECLARE @SKU varchar(50)=@Clave+'2';
    INSERT @IDs EXEC dbo.sp_ProductoVarianteGuardar @D,@Producto,@Presentacion,NULL,@SKU,20,0; SELECT @V2=ID FROM @IDs; DELETE @IDs;
    EXEC dbo.sp_ProveedorProductoGuardar @D,@Proveedor,@V1;
    EXEC dbo.sp_ProveedorProductoGuardar @D,@Proveedor,@V2;
    INSERT @IDs EXEC dbo.sp_BodegaGuardar @D,@Clave,'Bodega prueba'; SELECT @B=ID FROM @IDs; DELETE @IDs;
    INSERT @IDs EXEC dbo.sp_UbicacionGuardar @D,@B,'A-01'; SELECT @U=ID FROM @IDs; DELETE @IDs;
    DECLARE @Compra dbo.TipoDetalleOrdenCompra;
    INSERT @Compra VALUES (@V1,15,4),(@V2,10,8);
    INSERT @IDs EXEC dbo.sp_OrdenCompraCrear @D,@Clave,@Proveedor,@Compra; SELECT @Orden=ID FROM @IDs; DELETE @IDs;
    SELECT @O1=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden AND ProductoVarianteID=@V1;
    SELECT @O2=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden AND ProductoVarianteID=@V2;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden AND Estado='PENDIENTE') THROW 54001,'Orden pendiente incorrecta.',1;
    DECLARE @Rec dbo.TipoDetalleRecepcion,@Fecha datetime=DATEADD(day,-2,GETDATE());
    INSERT @Rec VALUES (@O1,5,4,@U,'A',NULL),(@O2,10,8,@U,'C',NULL);
    INSERT @IDs EXEC dbo.sp_RecepcionCompraRegistrar @D,@Clave,@Orden,@Fecha,@Rec; SELECT @R1=ID FROM @IDs; DELETE @IDs;
    SELECT @L1=l.LoteInventarioID FROM dbo.LoteInventario l JOIN dbo.DetalleRecepcionCompra r ON r.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID WHERE r.RecepcionCompraID=@R1 AND r.DetalleOrdenCompraID=@O1;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden AND Estado='PARCIAL') THROW 54002,'Recepcion parcial incorrecta.',1;
    DELETE @Rec; INSERT @Rec VALUES (@O1,11,5,@U,'EXCESO',NULL);
    DECLARE @Rechazo bit=0,@Numero varchar(30)=@Clave+'X';
    BEGIN TRY EXEC dbo.sp_RecepcionCompraRegistrar @D,@Numero,@Orden,@Fecha,@Rec; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52051 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR EXISTS (SELECT 1 FROM dbo.RecepcionCompra WHERE NumeroRecepcion=@Numero) THROW 54003,'Se acepto sobre-recepcion o quedaron datos.',1;
    DELETE @Rec; INSERT @Rec VALUES (@O1,10,5,@U,'B',NULL); SET @Numero=@Clave+'2'; SET @Fecha=DATEADD(day,-1,GETDATE());
    INSERT @IDs EXEC dbo.sp_RecepcionCompraRegistrar @D,@Numero,@Orden,@Fecha,@Rec; SELECT @R2=ID FROM @IDs; DELETE @IDs;
    SELECT @L2=l.LoteInventarioID FROM dbo.LoteInventario l JOIN dbo.DetalleRecepcionCompra r ON r.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID WHERE r.RecepcionCompraID=@R2;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden AND Estado='COMPLETA') THROW 54004,'Segunda recepcion no completa orden.',1;
    IF (SELECT SUM(CantidadDisponible) FROM dbo.vw_InventarioActual WHERE ProductoVarianteID=@V1)<>15 THROW 54005,'Inventario recibido incorrecto.',1;
    PRINT 'OK: orden, recepciones parcial/completa, costo real, rechazo de exceso e inventario.';
    DECLARE @Detalle dbo.TipoDetalleFactura,@Vacio dbo.TipoDetalleFactura;
    INSERT @Detalle VALUES (@V1,12,10),(@V2,2,20);
    INSERT @IDs EXEC dbo.sp_CotizacionCrear @D,@Clave,@Cliente,@Detalle; SELECT @Cot=ID FROM @IDs; DELETE @IDs;
    SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaGenerar @D,@Clave,@Cliente,@Vacio,@Cot; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52040 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 THROW 54006,'Se facturo cotizacion sin confirmar.',1;
    EXEC dbo.sp_CotizacionConfirmar @D,@Cot;
    UPDATE dbo.ProductoVariante SET PrecioVentaActual=99 WHERE ProductoVarianteID=@V1;
    INSERT @IDs EXEC dbo.sp_FacturaGenerar @D,@Clave,@Cliente,@Vacio,@Cot; SELECT @Factura=ID FROM @IDs; DELETE @IDs;
    IF NOT EXISTS (SELECT 1 FROM dbo.Factura WHERE FacturaID=@Factura AND TotalOriginal=160 AND Estado='PENDIENTE_PAGO') THROW 54007,'Total historico incorrecto.',1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Cotizacion WHERE CotizacionID=@Cot AND Estado='FACTURADA') THROW 54008,'Cotizacion no marcada facturada.',1;
    IF (SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@L1)<>0 OR (SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@L2)<>3 THROW 54009,'PEPS no distribuyo 5 y 7.',1;
    IF NOT EXISTS (SELECT 1 FROM dbo.DetalleFacturaLote fl JOIN dbo.DetalleFactura d ON d.DetalleFacturaID=fl.DetalleFacturaID WHERE d.FacturaID=@Factura AND fl.LoteInventarioID=@L1 AND fl.CantidadSalida=5)
        OR NOT EXISTS (SELECT 1 FROM dbo.DetalleFacturaLote fl JOIN dbo.DetalleFactura d ON d.DetalleFacturaID=fl.DetalleFacturaID WHERE d.FacturaID=@Factura AND fl.LoteInventarioID=@L2 AND fl.CantidadSalida=7) THROW 54010,'Detalle por lote incorrecto.',1;
    SET @Rechazo=0; SET @Numero=@Clave+'RE';
    BEGIN TRY EXEC dbo.sp_FacturaGenerar @D,@Numero,@Cliente,@Vacio,@Cot; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52040 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 THROW 54011,'Se reutilizo cotizacion.',1;
    DELETE @Detalle; INSERT @Detalle VALUES (@V1,4,10); SET @Numero=@Clave+'NS'; SET @Rechazo=0;
    DECLARE @Movimientos int=(SELECT COUNT(*) FROM dbo.MovimientoInventario);
    BEGIN TRY EXEC dbo.sp_FacturaGenerar @D,@Numero,@Cliente,@Detalle; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52041 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR EXISTS (SELECT 1 FROM dbo.Factura WHERE NumeroFactura=@Numero) OR (SELECT COUNT(*) FROM dbo.MovimientoInventario)<>@Movimientos OR (SELECT CantidadDisponible FROM dbo.LoteInventario WHERE LoteInventarioID=@L2)<>3 THROW 54012,'Fallo rollback por falta de stock.',1;
    DELETE @Detalle; INSERT @Detalle VALUES (@V1,1,10); SET @Numero=@Clave+'DIR';
    INSERT @IDs EXEC dbo.sp_FacturaGenerar @D,@Numero,@Cliente,@Detalle; SELECT @Directa=ID FROM @IDs; DELETE @IDs;
    IF NOT EXISTS (SELECT 1 FROM dbo.Factura WHERE FacturaID=@Directa AND CotizacionID IS NULL AND TotalOriginal=10) THROW 54013,'Factura directa incorrecta.',1;
    PRINT 'OK: cotizacion, confirmacion, precio historico, factura directa, multiples productos, PEPS y rollback.';
    DECLARE @Pagos dbo.TipoPagoFactura,@Efectivo int=(SELECT MedioPagoID FROM dbo.MedioPago WHERE Nombre='Efectivo'),@Tarjeta int=(SELECT MedioPagoID FROM dbo.MedioPago WHERE Nombre='Tarjeta');
    INSERT @Pagos VALUES (@Efectivo,60,NULL),(@Tarjeta,100,'REFERENCIA-PRUEBA');
    SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaCobrar @D,@Factura,@Pagos; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52021 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 THROW 54014,'Digitador pudo cobrar.',1;
    SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaGenerar @C,@Numero,@Cliente,@Detalle; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52021 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 THROW 54015,'Cajero pudo emitir.',1;
    UPDATE @Pagos SET Monto=59 WHERE MedioPagoID=@Efectivo; SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaCobrar @C,@Factura,@Pagos; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52042 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR EXISTS (SELECT 1 FROM dbo.PagoFactura WHERE FacturaID=@Factura) THROW 54016,'Cobro desbalanceado dejo datos.',1;
    UPDATE @Pagos SET Monto=60 WHERE MedioPagoID=@Efectivo;
    EXEC dbo.sp_FacturaCobrar @C,@Factura,@Pagos;
    IF (SELECT COUNT(*) FROM dbo.PagoFactura WHERE FacturaID=@Factura)<>2 OR NOT EXISTS (SELECT 1 FROM dbo.Factura WHERE FacturaID=@Factura AND Estado='PAGADA') THROW 54017,'Pago multiple incorrecto.',1;
    SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaCobrar @C,@Factura,@Pagos; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52042 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR (SELECT COUNT(*) FROM dbo.PagoFactura WHERE FacturaID=@Factura)<>2 THROW 54018,'Cobro duplicado.',1;
    IF EXISTS (SELECT 1 FROM dbo.LoteInventario WHERE CantidadDisponible<0) THROW 54019,'Stock negativo.',1;
    IF EXISTS (SELECT 1 FROM dbo.LoteInventario l WHERE l.LoteInventarioID IN (@L1,@L2) AND l.CantidadDisponible<>(SELECT SUM(m.Cantidad) FROM dbo.MovimientoInventario m WHERE m.LoteInventarioID=l.LoteInventarioID)) THROW 54020,'Movimientos no concuerdan con existencia.',1;
    -- Ejemplo obligatorio: 100 solicitadas, 75 recibidas, rechazar 30, completar con 25.
    DECLARE @Orden100 int,@Linea100 int,@Recibo100 int;
    DELETE @Compra; INSERT @Compra VALUES (@V2,100,8); SET @Numero=@Clave+'100';
    INSERT @IDs EXEC dbo.sp_OrdenCompraCrear @D,@Numero,@Proveedor,@Compra; SELECT @Orden100=ID FROM @IDs; DELETE @IDs;
    SELECT @Linea100=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden100;
    DELETE @Rec; INSERT @Rec VALUES (@Linea100,75,9,@U,'PARCIAL-75',NULL);
    INSERT @IDs EXEC dbo.sp_RecepcionCompraRegistrar @D,@Numero,@Orden100,@Fecha,@Rec; SELECT @Recibo100=ID FROM @IDs; DELETE @IDs;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden100 AND Estado='PARCIAL') THROW 54021,'100/75 no queda PARCIAL.',1;
    IF NOT EXISTS (SELECT 1 FROM dbo.LoteInventario l JOIN dbo.DetalleRecepcionCompra d ON d.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID
        WHERE d.RecepcionCompraID=@Recibo100 AND l.CantidadInicial=75 AND l.CantidadDisponible=75 AND l.CostoUnitario=9 AND l.UbicacionID=@U AND l.FechaIngreso=@Fecha)
        THROW 54022,'Recepcion no conserva costo real, cantidad, ubicacion o fecha.',1;
    UPDATE @Rec SET CantidadRecibida=30; SET @Numero=@Clave+'130'; SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_RecepcionCompraRegistrar @D,@Numero,@Orden100,@Fecha,@Rec; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52051 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR EXISTS (SELECT 1 FROM dbo.RecepcionCompra WHERE NumeroRecepcion=@Numero)
        OR (SELECT SUM(CantidadRecibida) FROM dbo.DetalleRecepcionCompra WHERE DetalleOrdenCompraID=@Linea100)<>75 THROW 54023,'Se acepto 75+30 sobre 100.',1;
    UPDATE @Rec SET CantidadRecibida=25; SET @Numero=@Clave+'125';
    EXEC dbo.sp_RecepcionCompraRegistrar @D,@Numero,@Orden100,@Fecha,@Rec;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden100 AND Estado='COMPLETA')
        OR (SELECT SUM(CantidadRecibida) FROM dbo.DetalleRecepcionCompra WHERE DetalleOrdenCompraID=@Linea100)<>100 THROW 54024,'75+25 no completa 100.',1;
    PRINT 'OK: ejemplo obligatorio 100 -> 75 -> 25; 75+30 rechazado sin cambios.';
    DECLARE @Cheque int=(SELECT MedioPagoID FROM dbo.MedioPago WHERE Nombre='Cheque');
    DELETE @Pagos; INSERT @Pagos VALUES (@Cheque,10,NULL); SET @Rechazo=0;
    BEGIN TRY EXEC dbo.sp_FacturaCobrar @C,@Directa,@Pagos; END TRY
    BEGIN CATCH IF ERROR_NUMBER()<>52042 THROW; SET @Rechazo=1; END CATCH;
    IF @Rechazo=0 OR EXISTS (SELECT 1 FROM dbo.PagoFactura WHERE FacturaID=@Directa) THROW 54025,'Cheque sin referencia aceptado.',1;
    UPDATE @Pagos SET Referencia='CHEQUE-PRUEBA'; EXEC dbo.sp_FacturaCobrar @C,@Directa,@Pagos;
    IF NOT EXISTS (SELECT 1 FROM dbo.PagoFactura WHERE FacturaID=@Directa AND MedioPagoID=@Cheque AND UsuarioCajeroID=@C)
        OR NOT EXISTS (SELECT 1 FROM dbo.Factura WHERE FacturaID=@Directa AND UsuarioEmisorID=@D) THROW 54026,'Cheque o separacion emisor/cajero incorrectos.',1;
    PRINT 'OK: cheque exige referencia; emisor Digitador y cobrador Cajero separados.';
    EXEC dbo.sp_CotizacionListar @G,@Clave;
    EXEC dbo.sp_CotizacionDetalle @G,@Cot;
    EXEC dbo.sp_FacturaListar @C,@Clave;
    EXEC dbo.sp_FacturaDetalle @C,@Factura;
    EXEC dbo.sp_FacturaPagos @C,@Factura;
    EXEC dbo.sp_OrdenCompraDetalle @D,@Orden;
    EXEC dbo.sp_RecepcionCompraListar @D,@Orden;
    EXEC dbo.sp_InventarioListar @D,@Clave;
    PRINT 'OK: pagos multiples exactos, permisos, cobro duplicado rechazado, consultas y stock nunca negativo.';
    ROLLBACK TRANSACTION;
    PRINT 'OK Fase II: todas las comprobaciones; datos temporales revertidos.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
