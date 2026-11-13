USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.sp_FacturaGenerar
    @UsuarioActorID int, @NumeroFactura varchar(30), @ClienteID int,
    @Detalles dbo.TipoDetalleFactura READONLY, @CotizacionID int=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION Fase2;
        EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
        IF NULLIF(LTRIM(RTRIM(@NumeroFactura)),'') IS NULL THROW 52040, 'Numero de factura obligatorio.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Cliente WHERE ClienteID=@ClienteID AND Activo=1) THROW 52040, 'Cliente no habilitado.', 1;
        DECLARE @Lineas TABLE (ProductoVarianteID int PRIMARY KEY,Cantidad decimal(18,4),PrecioUnitario decimal(18,2));
        IF @CotizacionID IS NULL
            INSERT @Lineas SELECT ProductoVarianteID,Cantidad,PrecioUnitario FROM @Detalles;
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM @Detalles) THROW 52040, 'Una factura de cotizacion utiliza exclusivamente su detalle historico.', 1;
            DECLARE @Estado varchar(30),@ClienteCotizacion int,@Vencimiento date;
            SELECT @Estado=Estado,@ClienteCotizacion=ClienteID,@Vencimiento=FechaVencimiento
            FROM dbo.Cotizacion WITH (UPDLOCK,HOLDLOCK) WHERE CotizacionID=@CotizacionID;
            IF @Estado IS NULL OR @Estado<>'CONFIRMADA' OR @ClienteCotizacion<>@ClienteID OR @Vencimiento<CONVERT(date,GETDATE())
                THROW 52040, 'Cotizacion no confirmada, vencida o de otro cliente.', 1;
            IF EXISTS (SELECT 1 FROM dbo.Factura WHERE CotizacionID=@CotizacionID) THROW 52040, 'Cotizacion ya facturada.', 1;
            INSERT @Lineas SELECT ProductoVarianteID,Cantidad,PrecioUnitario FROM dbo.DetalleCotizacion WHERE CotizacionID=@CotizacionID;
        END;
        IF NOT EXISTS (SELECT 1 FROM @Lineas) OR EXISTS (SELECT 1 FROM @Lineas WHERE Cantidad<=0 OR PrecioUnitario<0)
            THROW 52040, 'Detalle obligatorio con cantidades positivas y precios no negativos.', 1;
        -- Bloquear variantes en orden estable. Recepcion usa el mismo protocolo.
        -- Mantiene la validacion de stock y las salidas protegidas hasta COMMIT.
        DECLARE @Variante int=0,@Bloqueo int,@Cantidad decimal(18,4),@Disponible decimal(18,4);
        WHILE EXISTS (SELECT 1 FROM @Lineas WHERE ProductoVarianteID>@Variante)
        BEGIN
            SELECT @Variante=MIN(ProductoVarianteID) FROM @Lineas WHERE ProductoVarianteID>@Variante;
            SET @Bloqueo=NULL;
            SELECT @Bloqueo=ProductoVarianteID FROM dbo.ProductoVariante WITH (UPDLOCK,HOLDLOCK) WHERE ProductoVarianteID=@Variante;
            IF @Bloqueo IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.vw_VariantesDetalle WHERE ProductoVarianteID=@Variante AND Activo=1 AND ProductoActivo=1 AND PresentacionActiva=1 AND ColorActivo=1)
                THROW 52040, 'Variante no habilitada.', 1;
            SELECT @Cantidad=Cantidad FROM @Lineas WHERE ProductoVarianteID=@Variante;
            SELECT @Disponible=ISNULL(SUM(CantidadDisponible),0) FROM dbo.vw_InventarioActual WHERE ProductoVarianteID=@Variante;
            IF @Disponible<@Cantidad THROW 52041, 'Existencia insuficiente; factura no registrada.', 1;
        END;
        IF EXISTS (SELECT 1 FROM @Lineas WHERE ProductoVarianteID<=0) THROW 52040, 'Variante invalida.', 1;
        DECLARE @Total decimal(18,2)=(SELECT SUM(ROUND(Cantidad*PrecioUnitario,2)) FROM @Lineas);
        INSERT dbo.Factura (NumeroFactura,ClienteID,CotizacionID,UsuarioEmisorID,Fecha,Estado,TotalOriginal,TotalActual)
        VALUES (@NumeroFactura,@ClienteID,@CotizacionID,@UsuarioActorID,GETDATE(),CASE WHEN @Total=0 THEN 'PAGADA' ELSE 'PENDIENTE_PAGO' END,@Total,@Total);
        DECLARE @FacturaID int=SCOPE_IDENTITY(),@DetalleID int,@LoteID int,@PEPS bit,@Precio decimal(18,2),@Salida decimal(18,4);
        WHILE EXISTS (SELECT 1 FROM @Lineas)
        BEGIN
            SELECT TOP (1) @Variante=ProductoVarianteID,@Cantidad=Cantidad,@Precio=PrecioUnitario FROM @Lineas ORDER BY ProductoVarianteID;
            INSERT dbo.DetalleFactura (FacturaID,ProductoVarianteID,Cantidad,PrecioUnitario) VALUES (@FacturaID,@Variante,@Cantidad,@Precio);
            SET @DetalleID=SCOPE_IDENTITY();
            SELECT @PEPS=AplicaPEPS FROM dbo.vw_VariantesDetalle WHERE ProductoVarianteID=@Variante;
            WHILE @Cantidad>0
            BEGIN
                SET @LoteID=NULL;
                SELECT TOP (1) @LoteID=LoteInventarioID,@Disponible=CantidadDisponible
                FROM dbo.vw_InventarioActual WHERE ProductoVarianteID=@Variante AND CantidadDisponible>0
                ORDER BY CASE WHEN @PEPS=1 THEN FechaIngreso END ASC,LoteInventarioID ASC;
                IF @LoteID IS NULL THROW 52041, 'Existencia insuficiente durante distribucion.', 1;
                SET @Salida=CASE WHEN @Disponible<@Cantidad THEN @Disponible ELSE @Cantidad END;
                UPDATE dbo.LoteInventario SET CantidadDisponible=CantidadDisponible-@Salida
                WHERE LoteInventarioID=@LoteID AND CantidadDisponible>=@Salida;
                IF @@ROWCOUNT<>1 THROW 52041, 'La existencia cambio; operacion revertida.', 1;
                INSERT dbo.DetalleFacturaLote (DetalleFacturaID,LoteInventarioID,CantidadSalida) VALUES (@DetalleID,@LoteID,@Salida);
                INSERT dbo.MovimientoInventario (LoteInventarioID,UsuarioID,FechaHora,TipoMovimiento,Cantidad,Referencia)
                VALUES (@LoteID,@UsuarioActorID,GETDATE(),'SALIDA_VENTA',-@Salida,@NumeroFactura);
                SET @Cantidad=@Cantidad-@Salida;
            END;
            DELETE FROM @Lineas WHERE ProductoVarianteID=@Variante;
        END;
        IF @CotizacionID IS NOT NULL UPDATE dbo.Cotizacion SET Estado='FACTURADA' WHERE CotizacionID=@CotizacionID;
        INSERT dbo.Bitacora (UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
        VALUES (@UsuarioActorID,GETDATE(),'EMITIR_FACTURA','Factura',CONVERT(varchar(100),@FacturaID),@NumeroFactura);
        IF @Propia=1 COMMIT TRANSACTION;
        SELECT @FacturaID;
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
CREATE PROCEDURE dbo.sp_FacturaCobrar @UsuarioActorID int, @FacturaID int, @Pagos dbo.TipoPagoFactura READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    DECLARE @Propia bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        ELSE SAVE TRANSACTION Fase2;
        EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'COBRO';
        DECLARE @Total decimal(18,2),@Estado varchar(30);
        SELECT @Total=TotalActual,@Estado=Estado FROM dbo.Factura WITH (UPDLOCK,HOLDLOCK) WHERE FacturaID=@FacturaID;
        IF @Estado IS NULL OR @Estado<>'PENDIENTE_PAGO' OR EXISTS (SELECT 1 FROM dbo.PagoFactura WHERE FacturaID=@FacturaID)
            THROW 52042, 'Factura inexistente o cobro ya registrado.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Pagos) OR EXISTS (SELECT 1 FROM @Pagos WHERE Monto<=0)
            THROW 52042, 'Pagos obligatorios y positivos.', 1;
        IF EXISTS (SELECT 1 FROM @Pagos p LEFT JOIN dbo.MedioPago m ON m.MedioPagoID=p.MedioPagoID
            WHERE m.MedioPagoID IS NULL OR m.Activo=0 OR m.Nombre NOT IN ('Efectivo','Cheque','Tarjeta')
               OR (m.Nombre IN ('Cheque','Tarjeta') AND NULLIF(LTRIM(RTRIM(p.Referencia)),'') IS NULL))
            THROW 52042, 'Medio de pago invalido o referencia de cheque/tarjeta vacia.', 1;
        IF (SELECT SUM(Monto) FROM @Pagos)<>@Total THROW 52042, 'La suma de pagos debe coincidir exactamente con el total.', 1;
        INSERT dbo.PagoFactura (FacturaID,MedioPagoID,UsuarioCajeroID,FechaHora,Monto,Referencia)
        SELECT @FacturaID,MedioPagoID,@UsuarioActorID,GETDATE(),Monto,Referencia FROM @Pagos;
        UPDATE dbo.Factura SET Estado='PAGADA' WHERE FacturaID=@FacturaID;
        INSERT dbo.Bitacora (UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
        VALUES (@UsuarioActorID,GETDATE(),'COBRAR_FACTURA','Factura',CONVERT(varchar(100),@FacturaID),'PAGADA');
        IF @Propia=1 COMMIT TRANSACTION;
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
CREATE PROCEDURE dbo.sp_FacturaListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'FACTURA_CONSULTA';
    SELECT f.FacturaID,f.NumeroFactura,f.ClienteID,c.NombreRazonSocial AS Cliente,c.NIT,f.CotizacionID,f.Fecha,f.Estado,
        f.TotalOriginal,f.TotalActual,e.Nombres+' '+e.Apellidos AS Emisor,u.NombreUsuario AS UsuarioEmisor
    FROM dbo.Factura f JOIN dbo.Cliente c ON c.ClienteID=f.ClienteID
    JOIN dbo.Usuario u ON u.UsuarioID=f.UsuarioEmisorID JOIN dbo.Empleado e ON e.EmpleadoID=u.EmpleadoID
    WHERE @Buscar IS NULL OR f.NumeroFactura LIKE '%'+@Buscar+'%' OR c.NombreRazonSocial LIKE '%'+@Buscar+'%'
        OR EXISTS(SELECT 1 FROM dbo.Cotizacion q WHERE q.CotizacionID=f.CotizacionID AND q.NumeroCotizacion LIKE '%'+@Buscar+'%')
    ORDER BY f.FacturaID DESC;
END;
GO
CREATE PROCEDURE dbo.sp_FacturaDetalle @UsuarioActorID int, @FacturaID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'FACTURA_CONSULTA';
    SELECT d.ProductoVarianteID,v.CodigoSKU,v.Producto,v.Presentacion,v.Color,d.Cantidad,d.PrecioUnitario,
        CONVERT(decimal(18,2),ROUND(d.Cantidad*d.PrecioUnitario,2)) AS Subtotal
    FROM dbo.DetalleFactura d JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    WHERE d.FacturaID=@FacturaID ORDER BY d.DetalleFacturaID;
END;
GO
CREATE PROCEDURE dbo.sp_FacturaPagos @UsuarioActorID int, @FacturaID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'FACTURA_CONSULTA';
    SELECT p.MedioPagoID,m.Nombre AS Medio,p.Monto,p.Referencia,p.FechaHora,
        u.NombreUsuario AS Cajero
    FROM dbo.PagoFactura p JOIN dbo.MedioPago m ON m.MedioPagoID=p.MedioPagoID
    JOIN dbo.Usuario u ON u.UsuarioID=p.UsuarioCajeroID
    WHERE p.FacturaID=@FacturaID ORDER BY p.PagoFacturaID;
END;
GO
