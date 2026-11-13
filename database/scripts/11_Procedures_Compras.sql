USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.sp_OrdenCompraCrear @UsuarioActorID int, @NumeroOrden varchar(30), @ProveedorID int,
    @Detalles dbo.TipoDetalleOrdenCompra READONLY, @Observaciones varchar(300)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION Fase2;
        EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
        IF NULLIF(LTRIM(RTRIM(@NumeroOrden)),'') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Proveedor WHERE ProveedorID=@ProveedorID AND Activo=1)
            THROW 52050, 'Numero y proveedor activo obligatorios.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Detalles) OR EXISTS (SELECT 1 FROM @Detalles WHERE CantidadSolicitada<=0 OR CostoUnitarioOrden<0)
            THROW 52050, 'Detalle con cantidades positivas y costos no negativos obligatorio.', 1;
        IF EXISTS (SELECT 1 FROM @Detalles d LEFT JOIN dbo.ProveedorProducto pp ON pp.ProveedorID=@ProveedorID AND pp.ProductoVarianteID=d.ProductoVarianteID
            LEFT JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
            WHERE pp.ProveedorProductoID IS NULL OR pp.Activo=0 OR v.Activo=0 OR v.ProductoActivo=0 OR v.PresentacionActiva=0 OR v.ColorActivo=0)
            THROW 52050, 'La variante debe estar habilitada y asociada al proveedor.', 1;
        INSERT dbo.OrdenCompra (NumeroOrden,ProveedorID,UsuarioID,Fecha,Estado,Observaciones)
        VALUES (@NumeroOrden,@ProveedorID,@UsuarioActorID,GETDATE(),'PENDIENTE',@Observaciones);
        DECLARE @ID int=SCOPE_IDENTITY();
        INSERT dbo.DetalleOrdenCompra (OrdenCompraID,ProductoVarianteID,CantidadSolicitada,CostoUnitarioOrden)
        SELECT @ID,ProductoVarianteID,CantidadSolicitada,CostoUnitarioOrden FROM @Detalles;
        IF @Propia=1 COMMIT TRANSACTION;
        SELECT @ID;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()=-1 ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE()=1
        BEGIN
            IF @Propia=1 ROLLBACK TRANSACTION;
            ELSE ROLLBACK TRANSACTION Fase2;
        END;
        THROW;
    END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_OrdenCompraListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT o.OrdenCompraID,o.NumeroOrden,o.ProveedorID,p.Nombre AS Proveedor,o.Fecha,o.Estado,o.Observaciones
    FROM dbo.OrdenCompra o JOIN dbo.Proveedor p ON p.ProveedorID=o.ProveedorID
    WHERE @Buscar IS NULL OR o.NumeroOrden LIKE '%'+@Buscar+'%' OR p.Nombre LIKE '%'+@Buscar+'%' ORDER BY o.OrdenCompraID DESC;
END;
GO
CREATE PROCEDURE dbo.sp_OrdenCompraDetalle @UsuarioActorID int, @OrdenCompraID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT d.DetalleOrdenCompraID,d.ProductoVarianteID,v.CodigoSKU,v.Producto,v.Presentacion,v.Color,
        d.CantidadSolicitada,d.CostoUnitarioOrden,ISNULL(SUM(r.CantidadRecibida),0) AS CantidadRecibida,
        d.CantidadSolicitada-ISNULL(SUM(r.CantidadRecibida),0) AS CantidadPendiente
    FROM dbo.DetalleOrdenCompra d JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    LEFT JOIN dbo.DetalleRecepcionCompra r ON r.DetalleOrdenCompraID=d.DetalleOrdenCompraID
    WHERE d.OrdenCompraID=@OrdenCompraID
    GROUP BY d.DetalleOrdenCompraID,d.ProductoVarianteID,v.CodigoSKU,v.Producto,v.Presentacion,v.Color,d.CantidadSolicitada,d.CostoUnitarioOrden
    ORDER BY d.DetalleOrdenCompraID;
END;
GO
CREATE PROCEDURE dbo.sp_RecepcionCompraRegistrar
    @UsuarioActorID int, @NumeroRecepcion varchar(30), @OrdenCompraID int, @FechaIngreso datetime,
    @Detalles dbo.TipoDetalleRecepcion READONLY, @Observaciones varchar(300)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION Fase2;
        EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
        DECLARE @Estado varchar(30);
        SELECT @Estado=Estado FROM dbo.OrdenCompra WITH (UPDLOCK,HOLDLOCK) WHERE OrdenCompraID=@OrdenCompraID;
        IF @Estado IS NULL OR @Estado NOT IN ('PENDIENTE','PARCIAL') THROW 52051, 'Orden no pendiente de recepcion.', 1;
        IF NULLIF(LTRIM(RTRIM(@NumeroRecepcion)),'') IS NULL OR @FechaIngreso IS NULL OR @FechaIngreso>GETDATE()
            THROW 52051, 'Numero y fecha de ingreso no futura obligatorios.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Detalles) OR EXISTS (SELECT 1 FROM @Detalles WHERE CantidadRecibida<=0 OR CostoUnitario<0 OR FechaVencimiento<CONVERT(date,@FechaIngreso))
            THROW 52051, 'Detalle invalido: cantidad positiva, costo no negativo y vencimiento posterior al ingreso.', 1;
        IF EXISTS (SELECT 1 FROM @Detalles r LEFT JOIN dbo.DetalleOrdenCompra d ON d.DetalleOrdenCompraID=r.DetalleOrdenCompraID
            WHERE d.DetalleOrdenCompraID IS NULL OR d.OrdenCompraID<>@OrdenCompraID
               OR r.CantidadRecibida+ISNULL((SELECT SUM(x.CantidadRecibida) FROM dbo.DetalleRecepcionCompra x WHERE x.DetalleOrdenCompraID=d.DetalleOrdenCompraID),0)>d.CantidadSolicitada)
            THROW 52051, 'Linea ajena a la orden o cantidad superior a lo pendiente.', 1;
        IF EXISTS (SELECT 1 FROM @Detalles d LEFT JOIN dbo.Ubicacion u ON u.UbicacionID=d.UbicacionID
            LEFT JOIN dbo.Bodega b ON b.BodegaID=u.BodegaID WHERE u.UbicacionID IS NULL OR u.Activo=0 OR b.Activo=0)
            THROW 52051, 'Seleccione ubicacion y bodega activas.', 1;
        DECLARE @Variante int=0,@Bloqueo int;
        WHILE EXISTS (SELECT 1 FROM @Detalles r JOIN dbo.DetalleOrdenCompra d ON d.DetalleOrdenCompraID=r.DetalleOrdenCompraID WHERE d.ProductoVarianteID>@Variante)
        BEGIN
            SELECT @Variante=MIN(d.ProductoVarianteID) FROM @Detalles r JOIN dbo.DetalleOrdenCompra d ON d.DetalleOrdenCompraID=r.DetalleOrdenCompraID WHERE d.ProductoVarianteID>@Variante;
            SELECT @Bloqueo=ProductoVarianteID FROM dbo.ProductoVariante WITH (UPDLOCK,HOLDLOCK) WHERE ProductoVarianteID=@Variante;
        END;
        IF EXISTS (SELECT 1 FROM @Detalles r JOIN dbo.DetalleOrdenCompra d ON d.DetalleOrdenCompraID=r.DetalleOrdenCompraID
            JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
            WHERE v.Activo=0 OR v.ProductoActivo=0 OR v.PresentacionActiva=0 OR v.ColorActivo=0 OR (v.ManejaLote=1 AND NULLIF(LTRIM(RTRIM(r.NumeroLoteProveedor)),'') IS NULL))
            THROW 52051, 'Variante deshabilitada o lote proveedor obligatorio.', 1;
        INSERT dbo.RecepcionCompra (NumeroRecepcion,OrdenCompraID,UsuarioID,FechaRecepcion,Observaciones)
        VALUES (@NumeroRecepcion,@OrdenCompraID,@UsuarioActorID,GETDATE(),@Observaciones);
        DECLARE @RecepcionID int=SCOPE_IDENTITY(),@Linea int=0,@DetalleID int,@LoteID int;
        WHILE EXISTS (SELECT 1 FROM @Detalles WHERE DetalleOrdenCompraID>@Linea)
        BEGIN
            SELECT @Linea=MIN(DetalleOrdenCompraID) FROM @Detalles WHERE DetalleOrdenCompraID>@Linea;
            INSERT dbo.DetalleRecepcionCompra (RecepcionCompraID,DetalleOrdenCompraID,CantidadRecibida)
            SELECT @RecepcionID,@Linea,CantidadRecibida FROM @Detalles WHERE DetalleOrdenCompraID=@Linea;
            SET @DetalleID=SCOPE_IDENTITY();
            INSERT dbo.LoteInventario (DetalleRecepcionCompraID,UbicacionID,NumeroLoteProveedor,FechaIngreso,FechaVencimiento,CantidadInicial,CantidadDisponible,CostoUnitario)
            SELECT @DetalleID,UbicacionID,NumeroLoteProveedor,@FechaIngreso,FechaVencimiento,CantidadRecibida,CantidadRecibida,CostoUnitario
            FROM @Detalles WHERE DetalleOrdenCompraID=@Linea;
            SET @LoteID=SCOPE_IDENTITY();
            INSERT dbo.MovimientoInventario (LoteInventarioID,UsuarioID,FechaHora,TipoMovimiento,Cantidad,Referencia)
            SELECT @LoteID,@UsuarioActorID,GETDATE(),'ENTRADA_COMPRA',CantidadRecibida,@NumeroRecepcion FROM @Detalles WHERE DetalleOrdenCompraID=@Linea;
        END;
        UPDATE dbo.OrdenCompra SET Estado=CASE WHEN EXISTS (
            SELECT 1 FROM dbo.DetalleOrdenCompra d WHERE d.OrdenCompraID=@OrdenCompraID
            AND d.CantidadSolicitada>ISNULL((SELECT SUM(r.CantidadRecibida) FROM dbo.DetalleRecepcionCompra r WHERE r.DetalleOrdenCompraID=d.DetalleOrdenCompraID),0))
            THEN 'PARCIAL' ELSE 'COMPLETA' END WHERE OrdenCompraID=@OrdenCompraID;
        INSERT dbo.Bitacora (UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
        VALUES (@UsuarioActorID,GETDATE(),'RECIBIR_COMPRA','RecepcionCompra',CONVERT(varchar(100),@RecepcionID),@NumeroRecepcion);
        IF @Propia=1 COMMIT TRANSACTION;
        SELECT @RecepcionID;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()=-1 ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE()=1
        BEGIN
            IF @Propia=1 ROLLBACK TRANSACTION;
            ELSE ROLLBACK TRANSACTION Fase2;
        END;
        THROW;
    END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_RecepcionCompraListar @UsuarioActorID int, @OrdenCompraID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT r.RecepcionCompraID,r.NumeroRecepcion,r.OrdenCompraID,r.FechaRecepcion,r.Observaciones,
        d.DetalleOrdenCompraID,d.CantidadRecibida,l.CostoUnitario,l.NumeroLoteProveedor,l.FechaIngreso,u.Codigo AS Ubicacion
    FROM dbo.RecepcionCompra r JOIN dbo.DetalleRecepcionCompra d ON d.RecepcionCompraID=r.RecepcionCompraID
    JOIN dbo.LoteInventario l ON l.DetalleRecepcionCompraID=d.DetalleRecepcionCompraID JOIN dbo.Ubicacion u ON u.UbicacionID=l.UbicacionID
    WHERE r.OrdenCompraID=@OrdenCompraID ORDER BY r.RecepcionCompraID,d.DetalleRecepcionCompraID;
END;
GO
