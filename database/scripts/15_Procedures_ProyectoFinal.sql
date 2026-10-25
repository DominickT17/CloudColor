USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.sp_ReporteVentas
    @UsuarioActorID int,@FechaInicio date,@FechaFin date
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    IF @FechaInicio IS NULL OR @FechaFin IS NULL OR @FechaInicio>@FechaFin
        THROW 52100,'Indique un rango de fechas valido.',1;
    DECLARE @Total decimal(18,2),@Efectivo decimal(18,2),@Cheque decimal(18,2),@Tarjeta decimal(18,2),@Facturas int;
    SELECT @Facturas=COUNT(*),@Total=ISNULL(SUM(Total),0)
    FROM
    (
        SELECT f.FacturaID,ISNULL(SUM(v.IngresoNeto),0) AS Total
        FROM dbo.Factura f
        LEFT JOIN dbo.vw_VentasDetalleNeto v ON v.FacturaID=f.FacturaID
        WHERE f.Fecha>=@FechaInicio AND f.Fecha<DATEADD(day,1,@FechaFin) AND f.Estado<>'ANULADA'
        GROUP BY f.FacturaID
    ) Ventas;
    SELECT
        @Efectivo=ISNULL(SUM(CASE WHEN m.Nombre='Efectivo' THEN p.Monto ELSE 0 END),0),
        @Cheque=ISNULL(SUM(CASE WHEN m.Nombre='Cheque' THEN p.Monto ELSE 0 END),0),
        @Tarjeta=ISNULL(SUM(CASE WHEN m.Nombre='Tarjeta' THEN p.Monto ELSE 0 END),0)
    FROM dbo.PagoFactura p JOIN dbo.MedioPago m ON m.MedioPagoID=p.MedioPagoID
    JOIN dbo.Factura f ON f.FacturaID=p.FacturaID
    WHERE f.Fecha>=@FechaInicio AND f.Fecha<DATEADD(day,1,@FechaFin) AND f.Estado<>'ANULADA';
    SELECT @Total AS TotalFacturado,@Efectivo AS TotalEfectivo,@Cheque AS TotalCheque,
        @Tarjeta AS TotalTarjeta,@Facturas AS CantidadFacturas;
END;
GO

CREATE PROCEDURE dbo.sp_ReporteProductos
    @UsuarioActorID int,@FechaInicio date,@FechaFin date,@Tipo varchar(20),@Limite int=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    IF @FechaInicio IS NULL OR @FechaFin IS NULL OR @FechaInicio>@FechaFin
        THROW 52101,'Indique un rango de fechas valido.',1;
    IF @Tipo NOT IN ('MAS_VENDIDOS','MAYORES_INGRESOS','MENOS_VENDIDOS')
        THROW 52101,'Tipo de reporte no valido.',1;
    IF @Limite IS NOT NULL AND @Limite NOT IN (0,5,10)
        THROW 52101,'El limite debe ser 5, 10 o 0 para todos.',1;
    ;WITH Ventas AS
    (
        SELECT ProductoVarianteID,SUM(CantidadNeta) AS CantidadVendida,SUM(IngresoNeto) AS Ingresos
        FROM dbo.vw_VentasDetalleNeto
        WHERE Fecha>=@FechaInicio AND Fecha<DATEADD(day,1,@FechaFin) AND Estado<>'ANULADA'
        GROUP BY ProductoVarianteID
    )
    SELECT TOP (ISNULL(NULLIF(@Limite,0),2147483647))
        v.ProductoVarianteID,v.Producto,v.CodigoSKU,v.Presentacion,v.Color,
        ISNULL(x.CantidadVendida,0) AS CantidadVendida,ISNULL(x.Ingresos,0) AS Ingresos
    FROM dbo.vw_VariantesDetalle v LEFT JOIN Ventas x ON x.ProductoVarianteID=v.ProductoVarianteID
    WHERE v.Activo=1 AND v.ProductoActivo=1 AND v.PresentacionActiva=1 AND v.ColorActivo=1
      AND (@Tipo='MENOS_VENDIDOS' OR ISNULL(x.CantidadVendida,0)>0)
    ORDER BY
        CASE WHEN @Tipo='MAS_VENDIDOS' THEN ISNULL(x.CantidadVendida,0) END DESC,
        CASE WHEN @Tipo='MAYORES_INGRESOS' THEN ISNULL(x.Ingresos,0) END DESC,
        CASE WHEN @Tipo='MENOS_VENDIDOS' THEN ISNULL(x.CantidadVendida,0) END ASC,
        v.Producto,v.CodigoSKU;
END;
GO

CREATE PROCEDURE dbo.sp_ReporteSinStock @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    SELECT v.ProductoVarianteID,v.Producto,v.CodigoSKU,v.Presentacion,v.Color,
        ISNULL(s.Disponible,0) AS Disponible,v.StockMinimo,
        CASE WHEN ISNULL(s.Disponible,0)<=0 THEN 'SIN STOCK' ELSE 'STOCK BAJO' END AS Estado,
        p.Nombre AS Proveedor
    FROM dbo.vw_VariantesDetalle v
    OUTER APPLY
    (
        SELECT SUM(i.CantidadDisponible) AS Disponible
        FROM dbo.vw_InventarioActual i WHERE i.ProductoVarianteID=v.ProductoVarianteID
    ) s
    OUTER APPLY
    (
        SELECT TOP (1) pr.Nombre
        FROM dbo.ProveedorProducto pp JOIN dbo.Proveedor pr ON pr.ProveedorID=pp.ProveedorID
        WHERE pp.ProductoVarianteID=v.ProductoVarianteID AND pp.Activo=1 AND pr.Activo=1
        ORDER BY pr.Nombre
    ) p
    WHERE v.Activo=1 AND v.ProductoActivo=1 AND v.PresentacionActiva=1 AND v.ColorActivo=1
      AND ISNULL(s.Disponible,0)<=v.StockMinimo
    ORDER BY CASE WHEN ISNULL(s.Disponible,0)<=0 THEN 0 ELSE 1 END,v.Producto,v.CodigoSKU;
END;
GO

CREATE PROCEDURE dbo.sp_FacturaLotes @UsuarioActorID int,@FacturaID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID,'FACTURA_CONSULTA';
    SELECT d.DetalleFacturaID,v.CodigoSKU,v.Producto,v.Presentacion,v.Color,
        l.NumeroLoteProveedor,l.FechaIngreso,u.Codigo AS Ubicacion,b.Nombre AS Bodega,
        fl.CantidadSalida,ISNULL(r.CantidadDevuelta,0) AS CantidadDevuelta,
        fl.CantidadSalida-ISNULL(r.CantidadDevuelta,0) AS CantidadNeta
    FROM dbo.DetalleFactura d
    JOIN dbo.DetalleFacturaLote fl ON fl.DetalleFacturaID=d.DetalleFacturaID
    JOIN dbo.LoteInventario l ON l.LoteInventarioID=fl.LoteInventarioID
    JOIN dbo.Ubicacion u ON u.UbicacionID=l.UbicacionID
    JOIN dbo.Bodega b ON b.BodegaID=u.BodegaID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    OUTER APPLY
    (
        SELECT SUM(dd.CantidadDevuelta) AS CantidadDevuelta
        FROM dbo.DetalleDevolucion dd WHERE dd.DetalleFacturaLoteID=fl.DetalleFacturaLoteID
    ) r
    WHERE d.FacturaID=@FacturaID
    ORDER BY d.DetalleFacturaID,l.FechaIngreso,l.LoteInventarioID;
END;
GO

CREATE PROCEDURE dbo.sp_FacturaAnular
    @UsuarioActorID int,@FacturaID int,@Motivo varchar(300)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION ProyectoFinal;
        EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),'') IS NULL
            THROW 52110,'El motivo de anulacion es obligatorio.',1;
        DECLARE @Numero varchar(30),@Estado varchar(30);
        SELECT @Numero=NumeroFactura,@Estado=Estado
        FROM dbo.Factura WITH (UPDLOCK,HOLDLOCK) WHERE FacturaID=@FacturaID;
        IF @Estado IS NULL THROW 52110,'La factura indicada no existe.',1;
        IF @Estado='ANULADA' OR EXISTS(SELECT 1 FROM dbo.AnulacionFactura WHERE FacturaID=@FacturaID)
            THROW 52110,'La factura ya fue anulada.',1;
        DECLARE @Restaurar TABLE
        (
            DetalleFacturaLoteID int PRIMARY KEY,
            LoteInventarioID int NOT NULL,
            Cantidad decimal(18,4) NOT NULL
        );
        INSERT @Restaurar(DetalleFacturaLoteID,LoteInventarioID,Cantidad)
        SELECT fl.DetalleFacturaLoteID,fl.LoteInventarioID,
            fl.CantidadSalida-ISNULL(SUM(dd.CantidadDevuelta),0)
        FROM dbo.DetalleFactura d
        JOIN dbo.DetalleFacturaLote fl WITH (UPDLOCK,HOLDLOCK) ON fl.DetalleFacturaID=d.DetalleFacturaID
        LEFT JOIN dbo.DetalleDevolucion dd ON dd.DetalleFacturaLoteID=fl.DetalleFacturaLoteID
        WHERE d.FacturaID=@FacturaID
        GROUP BY fl.DetalleFacturaLoteID,fl.LoteInventarioID,fl.CantidadSalida
        HAVING fl.CantidadSalida-ISNULL(SUM(dd.CantidadDevuelta),0)>0;
        IF NOT EXISTS(SELECT 1 FROM dbo.DetalleFactura WHERE FacturaID=@FacturaID)
            THROW 52110,'La factura no tiene detalle para restaurar.',1;
        IF EXISTS
        (
            SELECT 1 FROM @Restaurar r JOIN dbo.LoteInventario l ON l.LoteInventarioID=r.LoteInventarioID
            WHERE l.CantidadDisponible+r.Cantidad>l.CantidadInicial
        ) THROW 52110,'No se puede restaurar el inventario original sin exceder un lote.',1;
        UPDATE l SET CantidadDisponible=l.CantidadDisponible+r.Cantidad
        FROM dbo.LoteInventario l JOIN @Restaurar r ON r.LoteInventarioID=l.LoteInventarioID;
        INSERT dbo.MovimientoInventario
            (LoteInventarioID,UsuarioID,FechaHora,TipoMovimiento,Cantidad,Referencia,Observaciones)
        SELECT LoteInventarioID,@UsuarioActorID,GETDATE(),'ENTRADA_ANULACION',Cantidad,@Numero,
            'Restitucion al anular la factura.'
        FROM @Restaurar;
        INSERT dbo.AnulacionFactura(FacturaID,UsuarioID,FechaHora,Motivo)
        VALUES(@FacturaID,@UsuarioActorID,GETDATE(),LTRIM(RTRIM(@Motivo)));
        UPDATE dbo.Factura SET Estado='ANULADA',TotalActual=0 WHERE FacturaID=@FacturaID;
        INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
        VALUES(@UsuarioActorID,GETDATE(),'ANULAR_FACTURA','Factura',CONVERT(varchar(100),@FacturaID),
            CONCAT(@Numero,' | Motivo: ',LTRIM(RTRIM(@Motivo))));
        IF @Propia=1 COMMIT TRANSACTION;
        SELECT @FacturaID AS FacturaID;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()=-1 ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE()=1
        BEGIN
            IF @Propia=1 ROLLBACK TRANSACTION;
            ELSE ROLLBACK TRANSACTION ProyectoFinal;
        END;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.sp_DevolucionDisponibles @UsuarioActorID int,@FacturaID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Factura f WHERE f.FacturaID=@FacturaID AND f.Estado<>'ANULADA'
          AND NOT EXISTS(SELECT 1 FROM dbo.AnulacionFactura a WHERE a.FacturaID=f.FacturaID)
    ) THROW 52120,'Seleccione una factura vigente.',1;
    SELECT fl.DetalleFacturaLoteID,d.DetalleFacturaID,d.ProductoVarianteID,v.CodigoSKU,
        v.Producto,v.Presentacion,v.Color,d.PrecioUnitario,fl.CantidadSalida,
        ISNULL(r.CantidadDevuelta,0) AS CantidadDevuelta,
        fl.CantidadSalida-ISNULL(r.CantidadDevuelta,0) AS CantidadDisponible,
        l.NumeroLoteProveedor,l.FechaIngreso,u.Codigo AS Ubicacion,b.Nombre AS Bodega
    FROM dbo.DetalleFactura d
    JOIN dbo.DetalleFacturaLote fl ON fl.DetalleFacturaID=d.DetalleFacturaID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    JOIN dbo.LoteInventario l ON l.LoteInventarioID=fl.LoteInventarioID
    JOIN dbo.Ubicacion u ON u.UbicacionID=l.UbicacionID
    JOIN dbo.Bodega b ON b.BodegaID=u.BodegaID
    OUTER APPLY
    (
        SELECT SUM(dd.CantidadDevuelta) AS CantidadDevuelta
        FROM dbo.DetalleDevolucion dd WHERE dd.DetalleFacturaLoteID=fl.DetalleFacturaLoteID
    ) r
    WHERE d.FacturaID=@FacturaID AND fl.CantidadSalida>ISNULL(r.CantidadDevuelta,0)
    ORDER BY d.DetalleFacturaID,l.FechaIngreso,l.LoteInventarioID;
END;
GO

CREATE PROCEDURE dbo.sp_DevolucionCrear
    @UsuarioActorID int,@NumeroDevolucion varchar(30),@FacturaID int,@Motivo varchar(300),
    @Detalles dbo.TipoDetalleDevolucion READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION ProyectoFinal;
        EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
        IF NULLIF(LTRIM(RTRIM(@NumeroDevolucion)),'') IS NULL
            THROW 52121,'El numero de devolucion es obligatorio.',1;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),'') IS NULL
            THROW 52121,'El motivo de devolucion es obligatorio.',1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) OR EXISTS(SELECT 1 FROM @Detalles WHERE CantidadDevuelta<=0)
            THROW 52121,'Seleccione al menos una cantidad positiva para devolver.',1;
        DECLARE @Estado varchar(30);
        SELECT @Estado=Estado FROM dbo.Factura WITH (UPDLOCK,HOLDLOCK) WHERE FacturaID=@FacturaID;
        IF @Estado IS NULL THROW 52121,'La factura indicada no existe.',1;
        IF @Estado='ANULADA' OR EXISTS(SELECT 1 FROM dbo.AnulacionFactura WHERE FacturaID=@FacturaID)
            THROW 52121,'No se puede devolver una factura anulada.',1;
        DECLARE @Validar TABLE
        (
            DetalleFacturaLoteID int PRIMARY KEY,
            DetalleFacturaID int NOT NULL,
            LoteInventarioID int NOT NULL,
            CantidadSalida decimal(18,4) NOT NULL,
            DevueltaAntes decimal(18,4) NOT NULL,
            CantidadNueva decimal(18,4) NOT NULL
        );
        INSERT @Validar
        SELECT fl.DetalleFacturaLoteID,fl.DetalleFacturaID,fl.LoteInventarioID,fl.CantidadSalida,
            ISNULL(r.CantidadDevuelta,0),x.CantidadDevuelta
        FROM @Detalles x
        JOIN dbo.DetalleFacturaLote fl WITH (UPDLOCK,HOLDLOCK) ON fl.DetalleFacturaLoteID=x.DetalleFacturaLoteID
        JOIN dbo.DetalleFactura d ON d.DetalleFacturaID=fl.DetalleFacturaID AND d.FacturaID=@FacturaID
        OUTER APPLY
        (
            SELECT SUM(dd.CantidadDevuelta) AS CantidadDevuelta
            FROM dbo.DetalleDevolucion dd WHERE dd.DetalleFacturaLoteID=fl.DetalleFacturaLoteID
        ) r;
        IF (SELECT COUNT(*) FROM @Validar)<>(SELECT COUNT(*) FROM @Detalles)
            THROW 52121,'Una linea seleccionada no pertenece a esta factura.',1;
        IF EXISTS(SELECT 1 FROM @Validar WHERE CantidadNueva>CantidadSalida-DevueltaAntes)
            THROW 52121,'La cantidad excede lo vendido disponible para devolucion.',1;
        IF EXISTS
        (
            SELECT 1 FROM @Validar v JOIN dbo.LoteInventario l ON l.LoteInventarioID=v.LoteInventarioID
            WHERE l.CantidadDisponible+v.CantidadNueva>l.CantidadInicial
        ) THROW 52121,'La cantidad no puede restaurarse al lote original.',1;
        INSERT dbo.Devolucion(NumeroDevolucion,FacturaID,UsuarioID,FechaHora,Motivo)
        VALUES(@NumeroDevolucion,@FacturaID,@UsuarioActorID,GETDATE(),LTRIM(RTRIM(@Motivo)));
        DECLARE @DevolucionID int=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.DetalleDevolucion(DevolucionID,DetalleFacturaLoteID,CantidadDevuelta)
        SELECT @DevolucionID,DetalleFacturaLoteID,CantidadNueva FROM @Validar;
        UPDATE l SET CantidadDisponible=l.CantidadDisponible+v.CantidadNueva
        FROM dbo.LoteInventario l JOIN @Validar v ON v.LoteInventarioID=l.LoteInventarioID;
        INSERT dbo.MovimientoInventario
            (LoteInventarioID,UsuarioID,FechaHora,TipoMovimiento,Cantidad,Referencia,Observaciones)
        SELECT LoteInventarioID,@UsuarioActorID,GETDATE(),'ENTRADA_DEVOLUCION',CantidadNueva,
            @NumeroDevolucion,'Restitucion parcial o total de una venta.'
        FROM @Validar;
        INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
        VALUES(@UsuarioActorID,GETDATE(),'REGISTRAR_DEVOLUCION','Devolucion',CONVERT(varchar(100),@DevolucionID),
            CONCAT(@NumeroDevolucion,' | FacturaID: ',@FacturaID,' | Motivo: ',LTRIM(RTRIM(@Motivo))));
        IF @Propia=1 COMMIT TRANSACTION;
        SELECT @DevolucionID AS DevolucionID;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()=-1 ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE()=1
        BEGIN
            IF @Propia=1 ROLLBACK TRANSACTION;
            ELSE ROLLBACK TRANSACTION ProyectoFinal;
        END;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.sp_DevolucionListar @UsuarioActorID int,@Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    SELECT d.DevolucionID,d.NumeroDevolucion,d.FacturaID,f.NumeroFactura,d.FechaHora,d.Motivo,
        u.NombreUsuario AS Usuario,COUNT(dd.DetalleDevolucionID) AS Lineas,
        SUM(dd.CantidadDevuelta) AS CantidadDevuelta
    FROM dbo.Devolucion d JOIN dbo.Factura f ON f.FacturaID=d.FacturaID
    JOIN dbo.Usuario u ON u.UsuarioID=d.UsuarioID
    JOIN dbo.DetalleDevolucion dd ON dd.DevolucionID=d.DevolucionID
    WHERE @Buscar IS NULL OR d.NumeroDevolucion LIKE '%'+@Buscar+'%'
       OR f.NumeroFactura LIKE '%'+@Buscar+'%' OR d.Motivo LIKE '%'+@Buscar+'%'
    GROUP BY d.DevolucionID,d.NumeroDevolucion,d.FacturaID,f.NumeroFactura,d.FechaHora,d.Motivo,u.NombreUsuario
    ORDER BY d.DevolucionID DESC;
END;
GO

CREATE PROCEDURE dbo.sp_DevolucionDetalle @UsuarioActorID int,@DevolucionID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID,1;
    SELECT fl.DetalleFacturaLoteID,fd.DetalleFacturaID,fd.ProductoVarianteID,v.CodigoSKU,
        v.Producto,v.Presentacion,v.Color,fd.PrecioUnitario,fl.CantidadSalida,dd.CantidadDevuelta,
        CONVERT(decimal(18,4),0) AS CantidadDisponible,l.NumeroLoteProveedor,l.FechaIngreso,
        ub.Codigo AS Ubicacion,b.Nombre AS Bodega
    FROM dbo.Devolucion d JOIN dbo.Factura f ON f.FacturaID=d.FacturaID
    JOIN dbo.Usuario u ON u.UsuarioID=d.UsuarioID
    JOIN dbo.DetalleDevolucion dd ON dd.DevolucionID=d.DevolucionID
    JOIN dbo.DetalleFacturaLote fl ON fl.DetalleFacturaLoteID=dd.DetalleFacturaLoteID
    JOIN dbo.DetalleFactura fd ON fd.DetalleFacturaID=fl.DetalleFacturaID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=fd.ProductoVarianteID
    JOIN dbo.LoteInventario l ON l.LoteInventarioID=fl.LoteInventarioID
    JOIN dbo.Ubicacion ub ON ub.UbicacionID=l.UbicacionID
    JOIN dbo.Bodega b ON b.BodegaID=ub.BodegaID
    WHERE d.DevolucionID=@DevolucionID ORDER BY dd.DetalleDevolucionID;
END;
GO
