-- Datos comerciales reproducibles para la demostracion de Fase II.
-- Ejecutar despues de 00-11 y de completar la configuracion inicial del Gerente en la aplicacion.
-- No crea usuarios, no incluye contrasenas ni hashes y no modifica el esquema.
USE CloudColorDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @UsuarioActorID int;
SELECT TOP (1) @UsuarioActorID=u.UsuarioID
FROM dbo.Usuario u
JOIN dbo.Empleado e ON e.EmpleadoID=u.EmpleadoID
JOIN dbo.Rol r ON r.RolID=u.RolID
WHERE u.Activo=1 AND e.Activo=1 AND r.Activo=1 AND r.Nombre='Gerente'
ORDER BY u.UsuarioID;

IF @UsuarioActorID IS NULL
    THROW 52060, 'Configure primero el usuario Gerente desde CloudColor. El script no crea usuarios ni hashes.', 1;

-- Si se vuelve a ejecutar, se detiene antes de crear documentos o duplicar datos.
IF EXISTS (SELECT 1 FROM dbo.Cliente WHERE NombreRazonSocial IN ('Ferretería La Esperanza','Constructora Los Altos','Decoraciones Quetzal'))
 OR EXISTS (SELECT 1 FROM dbo.Proveedor WHERE Nombre IN ('Pinturas del Occidente','Sherwin Williams'))
 OR EXISTS (SELECT 1 FROM dbo.Producto WHERE Nombre IN ('Pintura Látex Premium','Barniz Transparente','Sellador Acrílico'))
 OR EXISTS (SELECT 1 FROM dbo.ProductoVariante WHERE CodigoSKU IN ('LAT-BLA-GAL','BAR-TRA-QT','SEL-ACR-GAL'))
 OR EXISTS (SELECT 1 FROM dbo.Bodega WHERE Codigo='BOD01')
 OR EXISTS (SELECT 1 FROM dbo.OrdenCompra WHERE NumeroOrden IN ('OC-2026-001','OC-2026-002','OC-2026-003'))
 OR EXISTS (SELECT 1 FROM dbo.RecepcionCompra WHERE NumeroRecepcion IN ('RC-2026-001','RC-2026-002','RC-2026-003','RC-2026-004'))
 OR EXISTS (SELECT 1 FROM dbo.Cotizacion WHERE NumeroCotizacion IN ('CT-2026-001','CT-2026-002'))
 OR EXISTS (SELECT 1 FROM dbo.Factura WHERE NumeroFactura IN ('FAC-2026-001','FAC-2026-002'))
    THROW 52061, 'Ya existen datos de esta demostracion. No se hicieron cambios para evitar duplicados.', 1;

IF (SELECT COUNT(*) FROM dbo.CategoriaProducto WHERE Nombre IN ('Pinturas','Barnices','Selladores'))<>3
    THROW 52062, 'Faltan categorias del catalogo inicial; ejecute 02_InitialData.sql.', 1;
IF (SELECT COUNT(*) FROM dbo.Presentacion WHERE Nombre IN ('1 galon','1/4 de galon'))<>2
    THROW 52062, 'Faltan presentaciones del catalogo inicial; ejecute 02_InitialData.sql.', 1;
IF (SELECT COUNT(*) FROM dbo.MedioPago WHERE Nombre IN ('Efectivo','Tarjeta'))<>2
    THROW 52062, 'Faltan medios de pago del catalogo inicial; ejecute 02_InitialData.sql.', 1;

DECLARE @Cliente1 int,@Cliente2 int,@Cliente3 int,@Proveedor1 int,@Proveedor2 int,
        @CategoriaPinturas int,@CategoriaBarnices int,@CategoriaSelladores int,
        @Marca int,@PresentacionGalon int,@PresentacionCuarto int,@ColorBlanco int,
        @ProductoLatex int,@ProductoBarniz int,@ProductoSellador int,
        @VarianteLatex int,@VarianteBarniz int,@VarianteSellador int,
        @Bodega int,@UbicacionA01 int,@UbicacionA02 int,
        @Orden1 int,@Orden2 int,@Orden3 int,@Cotizacion1 int,@Cotizacion2 int,
        @Factura1 int,@Factura2 int,@DetalleOrden1 int,@DetalleOrden2 int,@DetalleOrden3 int;
DECLARE @Fecha1 datetime=DATEADD(DAY,-2,GETDATE()), @Fecha2 datetime=DATEADD(DAY,-1,GETDATE());
DECLARE @IdResult TABLE (ID int);

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT @IdResult EXEC dbo.sp_ClienteInsertar @UsuarioActorID,'Empresa','Ferretería La Esperanza',NULL,NULL,NULL,'Quetzaltenango';
    INSERT @IdResult EXEC dbo.sp_ClienteInsertar @UsuarioActorID,'Empresa','Constructora Los Altos',NULL,NULL,NULL,'Quetzaltenango';
    INSERT @IdResult EXEC dbo.sp_ClienteInsertar @UsuarioActorID,'Empresa','Decoraciones Quetzal',NULL,NULL,NULL,'Salcajá, Quetzaltenango';
    SELECT @Cliente1=ClienteID FROM dbo.Cliente WHERE NombreRazonSocial='Ferretería La Esperanza';
    SELECT @Cliente2=ClienteID FROM dbo.Cliente WHERE NombreRazonSocial='Constructora Los Altos';
    SELECT @Cliente3=ClienteID FROM dbo.Cliente WHERE NombreRazonSocial='Decoraciones Quetzal';

    INSERT @IdResult EXEC dbo.sp_ProveedorInsertar @UsuarioActorID,'Pinturas del Occidente','5489213-7','77651234','ventas@pinturasoccidente.com','Quetzaltenango, Zona 1';
    INSERT @IdResult EXEC dbo.sp_ProveedorInsertar @UsuarioActorID,'Sherwin Williams','123456','88995500','ejemplo@gmail.com','Zona 1';
    SELECT @Proveedor1=ProveedorID FROM dbo.Proveedor WHERE Nombre='Pinturas del Occidente';
    SELECT @Proveedor2=ProveedorID FROM dbo.Proveedor WHERE Nombre='Sherwin Williams';

    SELECT @CategoriaPinturas=CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre='Pinturas';
    SELECT @CategoriaBarnices=CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre='Barnices';
    SELECT @CategoriaSelladores=CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre='Selladores';
    SELECT @PresentacionGalon=PresentacionID FROM dbo.Presentacion WHERE Nombre='1 galon';
    SELECT @PresentacionCuarto=PresentacionID FROM dbo.Presentacion WHERE Nombre='1/4 de galon';
    SELECT @Marca=MarcaID FROM dbo.Marca WHERE Nombre='Sherwin-Williams' AND Activo=1;
    IF @Marca IS NULL
    BEGIN
        DELETE FROM @IdResult;
        INSERT @IdResult EXEC dbo.sp_MarcaInsertar @UsuarioActorID,'Sherwin-Williams';
        SELECT @Marca=ID FROM @IdResult;
    END;
    SELECT @ColorBlanco=ColorID FROM dbo.Color WHERE Nombre='Blanco' AND Activo=1;
    IF @ColorBlanco IS NULL
    BEGIN
        DELETE FROM @IdResult;
        INSERT @IdResult EXEC dbo.sp_ColorCrear @UsuarioActorID,'Blanco','#FFFFFF';
        SELECT @ColorBlanco=ID FROM @IdResult;
    END;

    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_ProductoInsertar @UsuarioActorID,@CategoriaPinturas,@Marca,'Pintura Látex Premium','Pintura a base de agua para interiores',1,1;
    INSERT @IdResult EXEC dbo.sp_ProductoInsertar @UsuarioActorID,@CategoriaBarnices,@Marca,'Barniz Transparente','Acabado transparente para madera',1,1;
    INSERT @IdResult EXEC dbo.sp_ProductoInsertar @UsuarioActorID,@CategoriaSelladores,@Marca,'Sellador Acrílico','Preparación de superficies para pintura',1,1;
    SELECT @ProductoLatex=ProductoID FROM dbo.Producto WHERE Nombre='Pintura Látex Premium';
    SELECT @ProductoBarniz=ProductoID FROM dbo.Producto WHERE Nombre='Barniz Transparente';
    SELECT @ProductoSellador=ProductoID FROM dbo.Producto WHERE Nombre='Sellador Acrílico';

    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_ProductoVarianteGuardar @UsuarioActorID,@ProductoLatex,@PresentacionGalon,@ColorBlanco,'LAT-BLA-GAL',185,20,0;
    INSERT @IdResult EXEC dbo.sp_ProductoVarianteGuardar @UsuarioActorID,@ProductoBarniz,@PresentacionCuarto,NULL,'BAR-TRA-QT',95,10,0;
    INSERT @IdResult EXEC dbo.sp_ProductoVarianteGuardar @UsuarioActorID,@ProductoSellador,@PresentacionGalon,NULL,'SEL-ACR-GAL',125,10,0;
    SELECT @VarianteLatex=ProductoVarianteID FROM dbo.ProductoVariante WHERE CodigoSKU='LAT-BLA-GAL';
    SELECT @VarianteBarniz=ProductoVarianteID FROM dbo.ProductoVariante WHERE CodigoSKU='BAR-TRA-QT';
    SELECT @VarianteSellador=ProductoVarianteID FROM dbo.ProductoVariante WHERE CodigoSKU='SEL-ACR-GAL';

    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_BodegaGuardar @UsuarioActorID,'BOD01','Bodega Principal','Área de demostración',0;
    SELECT @Bodega=BodegaID FROM dbo.Bodega WHERE Codigo='BOD01';
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_UbicacionGuardar @UsuarioActorID,@Bodega,'A-01','Pinturas y recubrimientos',0;
    INSERT @IdResult EXEC dbo.sp_UbicacionGuardar @UsuarioActorID,@Bodega,'A-02','Barnices y selladores',0;
    SELECT @UbicacionA01=UbicacionID FROM dbo.Ubicacion WHERE BodegaID=@Bodega AND Codigo='A-01';
    SELECT @UbicacionA02=UbicacionID FROM dbo.Ubicacion WHERE BodegaID=@Bodega AND Codigo='A-02';

    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_ProveedorProductoGuardar @UsuarioActorID,@Proveedor1,@VarianteLatex,0;
    INSERT @IdResult EXEC dbo.sp_ProveedorProductoGuardar @UsuarioActorID,@Proveedor2,@VarianteBarniz,0;
    INSERT @IdResult EXEC dbo.sp_ProveedorProductoGuardar @UsuarioActorID,@Proveedor2,@VarianteSellador,0;

    DECLARE @OrdenLineas dbo.TipoDetalleOrdenCompra;
    INSERT @OrdenLineas VALUES (@VarianteLatex,100,130);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_OrdenCompraCrear @UsuarioActorID,'OC-2026-001',@Proveedor1,@OrdenLineas,'Demostración: recepción en dos entregas.';
    SELECT @Orden1=OrdenCompraID FROM dbo.OrdenCompra WHERE NumeroOrden='OC-2026-001';
    SELECT @DetalleOrden1=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden1 AND ProductoVarianteID=@VarianteLatex;

    DECLARE @Recepcion dbo.TipoDetalleRecepcion;
    INSERT @Recepcion VALUES (@DetalleOrden1,75,130,@UbicacionA01,'LAT-2610-01',NULL);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_RecepcionCompraRegistrar @UsuarioActorID,'RC-2026-001',@Orden1,@Fecha1,@Recepcion,'Primera entrega: 75 de 100.';
    DELETE FROM @Recepcion;
    INSERT @Recepcion VALUES (@DetalleOrden1,25,130,@UbicacionA01,'LAT-2610-02',NULL);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_RecepcionCompraRegistrar @UsuarioActorID,'RC-2026-002',@Orden1,@Fecha2,@Recepcion,'Entrega final: 25 de 100.';

    DELETE FROM @OrdenLineas;
    INSERT @OrdenLineas VALUES (@VarianteBarniz,60,65);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_OrdenCompraCrear @UsuarioActorID,'OC-2026-002',@Proveedor2,@OrdenLineas,'Demostración: orden parcialmente recibida.';
    SELECT @Orden2=OrdenCompraID FROM dbo.OrdenCompra WHERE NumeroOrden='OC-2026-002';
    SELECT @DetalleOrden2=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden2 AND ProductoVarianteID=@VarianteBarniz;
    DELETE FROM @Recepcion;
    INSERT @Recepcion VALUES (@DetalleOrden2,35,65,@UbicacionA02,'BAR-2610-01',NULL);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_RecepcionCompraRegistrar @UsuarioActorID,'RC-2026-003',@Orden2,@Fecha2,@Recepcion,'Recepción parcial: 35 de 60; quedan 25.';

    DELETE FROM @OrdenLineas;
    INSERT @OrdenLineas VALUES (@VarianteSellador,20,85);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_OrdenCompraCrear @UsuarioActorID,'OC-2026-003',@Proveedor2,@OrdenLineas,'Compra para la factura directa de demostración.';
    SELECT @Orden3=OrdenCompraID FROM dbo.OrdenCompra WHERE NumeroOrden='OC-2026-003';
    SELECT @DetalleOrden3=DetalleOrdenCompraID FROM dbo.DetalleOrdenCompra WHERE OrdenCompraID=@Orden3 AND ProductoVarianteID=@VarianteSellador;
    DELETE FROM @Recepcion;
    INSERT @Recepcion VALUES (@DetalleOrden3,20,85,@UbicacionA02,'SEL-2610-01',NULL);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_RecepcionCompraRegistrar @UsuarioActorID,'RC-2026-004',@Orden3,@Fecha2,@Recepcion,'Recepción completa: 20 de 20.';

    DECLARE @Detalle dbo.TipoDetalleFactura;
    INSERT @Detalle VALUES (@VarianteLatex,2,185),(@VarianteBarniz,1,95);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_CotizacionCrear @UsuarioActorID,'CT-2026-001',@Cliente1,@Detalle,NULL,'Cotización abierta para demostración.';

    DELETE FROM @Detalle;
    INSERT @Detalle VALUES (@VarianteLatex,80,185),(@VarianteBarniz,2,95);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_CotizacionCrear @UsuarioActorID,'CT-2026-002',@Cliente2,@Detalle,NULL,'Cotización confirmada y facturada.';
    SELECT @Cotizacion2=CotizacionID FROM dbo.Cotizacion WHERE NumeroCotizacion='CT-2026-002';
    EXEC dbo.sp_CotizacionConfirmar @UsuarioActorID,@Cotizacion2;

    DELETE FROM @Detalle;
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_FacturaGenerar @UsuarioActorID,'FAC-2026-001',@Cliente2,@Detalle,@Cotizacion2;
    SELECT @Factura1=FacturaID FROM dbo.Factura WHERE NumeroFactura='FAC-2026-001';

    DECLARE @Pagos dbo.TipoPagoFactura,@MedioEfectivo int,@MedioTarjeta int;
    SELECT @MedioEfectivo=MedioPagoID FROM dbo.MedioPago WHERE Nombre='Efectivo';
    SELECT @MedioTarjeta=MedioPagoID FROM dbo.MedioPago WHERE Nombre='Tarjeta';
    INSERT @Pagos VALUES (@MedioEfectivo,7495,NULL),(@MedioTarjeta,7495,'VISA-4582');
    EXEC dbo.sp_FacturaCobrar @UsuarioActorID,@Factura1,@Pagos;

    DELETE FROM @Detalle;
    INSERT @Detalle VALUES (@VarianteLatex,2,185),(@VarianteBarniz,1,95),(@VarianteSellador,1,125);
    DELETE FROM @IdResult;
    INSERT @IdResult EXEC dbo.sp_FacturaGenerar @UsuarioActorID,'FAC-2026-002',@Cliente3,@Detalle,NULL;
    SELECT @Factura2=FacturaID FROM dbo.Factura WHERE NumeroFactura='FAC-2026-002';

    IF (SELECT Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden1)<>'COMPLETA'
       OR (SELECT Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden2)<>'PARCIAL'
       OR (SELECT Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden3)<>'COMPLETA'
       OR (SELECT Estado FROM dbo.Cotizacion WHERE NumeroCotizacion='CT-2026-001')<>'PENDIENTE'
       OR (SELECT Estado FROM dbo.Cotizacion WHERE CotizacionID=@Cotizacion2)<>'FACTURADA'
       OR (SELECT TotalActual FROM dbo.Factura WHERE FacturaID=@Factura1)<>14990
       OR (SELECT Estado FROM dbo.Factura WHERE FacturaID=@Factura1)<>'PAGADA'
       OR (SELECT TotalActual FROM dbo.Factura WHERE FacturaID=@Factura2)<>590
       OR (SELECT Estado FROM dbo.Factura WHERE FacturaID=@Factura2)<>'PENDIENTE_PAGO'
       OR (SELECT SUM(d.Cantidad*d.PrecioUnitario) FROM dbo.DetalleCotizacion d JOIN dbo.Cotizacion c ON c.CotizacionID=d.CotizacionID WHERE c.NumeroCotizacion='CT-2026-001')<>465
       OR (SELECT COUNT(*) FROM dbo.RecepcionCompra WHERE NumeroRecepcion IN ('RC-2026-001','RC-2026-002','RC-2026-003','RC-2026-004'))<>4
       OR (SELECT SUM(r.CantidadRecibida) FROM dbo.DetalleRecepcionCompra r JOIN dbo.RecepcionCompra x ON x.RecepcionCompraID=r.RecepcionCompraID WHERE x.NumeroRecepcion='RC-2026-001')<>75
       OR (SELECT SUM(r.CantidadRecibida) FROM dbo.DetalleRecepcionCompra r JOIN dbo.RecepcionCompra x ON x.RecepcionCompraID=r.RecepcionCompraID WHERE x.NumeroRecepcion='RC-2026-002')<>25
       OR (SELECT COUNT(*) FROM dbo.PagoFactura p JOIN dbo.Factura f ON f.FacturaID=p.FacturaID WHERE f.NumeroFactura='FAC-2026-001')<>2
       OR (SELECT SUM(p.Monto) FROM dbo.PagoFactura p JOIN dbo.Factura f ON f.FacturaID=p.FacturaID WHERE f.NumeroFactura='FAC-2026-001')<>14990
       OR EXISTS (SELECT 1 FROM dbo.DetalleFacturaLote x JOIN dbo.DetalleFactura d ON d.DetalleFacturaID=x.DetalleFacturaID JOIN dbo.Factura f ON f.FacturaID=d.FacturaID JOIN dbo.LoteInventario l ON l.LoteInventarioID=x.LoteInventarioID WHERE f.NumeroFactura='FAC-2026-001' AND l.NumeroLoteProveedor='LAT-2610-01' AND x.CantidadSalida<>75)
       OR (SELECT SUM(CantidadDisponible) FROM dbo.vw_InventarioActual WHERE CodigoSKU='LAT-BLA-GAL')<>18
       OR (SELECT SUM(CantidadDisponible) FROM dbo.vw_InventarioActual WHERE CodigoSKU='BAR-TRA-QT')<>32
       OR (SELECT SUM(CantidadDisponible) FROM dbo.vw_InventarioActual WHERE CodigoSKU='SEL-ACR-GAL')<>19
        THROW 52063, 'La verificacion final de la demostracion no coincide con los resultados esperados.', 1;

    COMMIT TRANSACTION;
    PRINT 'Datos comerciales de demostración Fase II creados y verificados.';
    SELECT 'OC-2026-001' AS Orden,Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden1
    UNION ALL SELECT 'OC-2026-002',Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden2
    UNION ALL SELECT 'OC-2026-003',Estado FROM dbo.OrdenCompra WHERE OrdenCompraID=@Orden3;
    SELECT CodigoSKU,SUM(CantidadDisponible) AS ExistenciaDisponible
    FROM dbo.vw_InventarioActual WHERE CodigoSKU IN ('LAT-BLA-GAL','BAR-TRA-QT','SEL-ACR-GAL') GROUP BY CodigoSKU ORDER BY CodigoSKU;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
