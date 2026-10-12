USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.sp_CotizacionCrear
    @UsuarioActorID int, @NumeroCotizacion varchar(30), @ClienteID int,
    @Detalles dbo.TipoDetalleFactura READONLY, @FechaVencimiento date=NULL, @Observaciones varchar(300)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION Fase2;
        EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
        IF NULLIF(LTRIM(RTRIM(@NumeroCotizacion)),'') IS NULL THROW 52030, 'Numero de cotizacion obligatorio.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Cliente WHERE ClienteID=@ClienteID AND Activo=1) THROW 52030, 'Cliente no habilitado.', 1;
        IF @FechaVencimiento<CONVERT(date,GETDATE()) THROW 52030, 'Vencimiento anterior a hoy.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Detalles) OR EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad<=0 OR PrecioUnitario<0)
            THROW 52030, 'Detalle obligatorio, cantidades positivas y precios no negativos.', 1;
        IF EXISTS (SELECT 1 FROM @Detalles d LEFT JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
            WHERE v.ProductoVarianteID IS NULL OR v.Activo=0 OR v.ProductoActivo=0 OR v.PresentacionActiva=0 OR v.ColorActivo=0)
            THROW 52030, 'Variante no habilitada.', 1;
        INSERT dbo.Cotizacion (NumeroCotizacion,ClienteID,UsuarioID,Fecha,FechaVencimiento,Estado,Observaciones)
        VALUES (@NumeroCotizacion,@ClienteID,@UsuarioActorID,GETDATE(),@FechaVencimiento,'PENDIENTE',@Observaciones);
        DECLARE @ID int=SCOPE_IDENTITY();
        INSERT dbo.DetalleCotizacion (CotizacionID,ProductoVarianteID,Cantidad,PrecioUnitario)
        SELECT @ID,ProductoVarianteID,Cantidad,PrecioUnitario FROM @Detalles;
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
CREATE PROCEDURE dbo.sp_CotizacionConfirmar @UsuarioActorID int, @CotizacionID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    UPDATE dbo.Cotizacion SET Estado='CONFIRMADA' WHERE CotizacionID=@CotizacionID AND Estado='PENDIENTE'
        AND (FechaVencimiento IS NULL OR FechaVencimiento>=CONVERT(date,GETDATE()));
    IF @@ROWCOUNT=0 THROW 52030, 'Cotizacion inexistente, vencida o no pendiente.', 1;
END;
GO
CREATE PROCEDURE dbo.sp_CotizacionListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT c.CotizacionID,c.NumeroCotizacion,c.ClienteID,cl.NombreRazonSocial AS Cliente,c.Fecha,c.FechaVencimiento,c.Estado,c.Observaciones,
        CONVERT(decimal(18,2),SUM(ROUND(d.Cantidad*d.PrecioUnitario,2))) AS Total
    FROM dbo.Cotizacion c JOIN dbo.Cliente cl ON cl.ClienteID=c.ClienteID JOIN dbo.DetalleCotizacion d ON d.CotizacionID=c.CotizacionID
    WHERE @Buscar IS NULL OR c.NumeroCotizacion LIKE '%'+@Buscar+'%' OR cl.NombreRazonSocial LIKE '%'+@Buscar+'%'
    GROUP BY c.CotizacionID,c.NumeroCotizacion,c.ClienteID,cl.NombreRazonSocial,c.Fecha,c.FechaVencimiento,c.Estado,c.Observaciones
    ORDER BY c.CotizacionID DESC;
END;
GO
CREATE PROCEDURE dbo.sp_CotizacionDetalle @UsuarioActorID int, @CotizacionID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT d.ProductoVarianteID,v.CodigoSKU,v.Producto,v.Presentacion,v.Color,d.Cantidad,d.PrecioUnitario,
        CONVERT(decimal(18,2),ROUND(d.Cantidad*d.PrecioUnitario,2)) AS Subtotal
    FROM dbo.DetalleCotizacion d JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    WHERE d.CotizacionID=@CotizacionID ORDER BY d.DetalleCotizacionID;
END;
GO
