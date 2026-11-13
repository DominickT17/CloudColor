USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.sp_AutorizarFase2 @UsuarioActorID int, @Operacion varchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Operacion NOT IN ('INGRESO','COBRO','FACTURA_CONSULTA') OR @Operacion IS NULL
        THROW 52020, 'Operacion no reconocida.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Empleado e ON e.EmpleadoID=u.EmpleadoID
        JOIN dbo.Rol r ON r.RolID=u.RolID
        WHERE u.UsuarioID=@UsuarioActorID AND u.Activo=1 AND e.Activo=1 AND r.Activo=1
        AND (r.Nombre='Gerente' OR (r.Nombre='Digitador' AND @Operacion IN ('INGRESO','FACTURA_CONSULTA'))
             OR (r.Nombre='Cajero' AND @Operacion IN ('COBRO','FACTURA_CONSULTA'))))
        THROW 52021, 'Su perfil no permite esta operacion de Fase II.', 1;
END;
GO
CREATE PROCEDURE dbo.sp_ProductoVarianteListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT * FROM dbo.vw_VariantesDetalle
    WHERE @Buscar IS NULL OR CodigoSKU LIKE '%'+@Buscar+'%' OR Producto LIKE '%'+@Buscar+'%'
    ORDER BY Producto, CodigoSKU;
END;
GO
CREATE PROCEDURE dbo.sp_ProductoVarianteGuardar
    @UsuarioActorID int, @ProductoID int, @PresentacionID int, @ColorID int,
    @CodigoSKU varchar(50), @PrecioVentaActual decimal(18,2), @StockMinimo decimal(18,4), @ProductoVarianteID int=0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF NULLIF(LTRIM(RTRIM(@CodigoSKU)),'') IS NULL OR @PrecioVentaActual IS NULL OR @PrecioVentaActual<0 OR @StockMinimo IS NULL OR @StockMinimo<0
        THROW 52022, 'SKU obligatorio; precio y stock minimo no negativos.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Producto WHERE ProductoID=@ProductoID AND Activo=1)
        OR NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE PresentacionID=@PresentacionID AND Activo=1)
        OR (@ColorID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Color WHERE ColorID=@ColorID AND Activo=1))
        THROW 52022, 'Seleccione producto, presentacion y color habilitados.', 1;
    -- La identidad comercial de una variante utilizada debe conservar su historia.
    IF EXISTS (SELECT 1 FROM dbo.ProductoVariante v WHERE v.ProductoVarianteID=@ProductoVarianteID
        AND (v.ProductoID<>@ProductoID OR v.PresentacionID<>@PresentacionID OR ISNULL(v.ColorID,0)<>ISNULL(@ColorID,0)))
       AND (EXISTS (SELECT 1 FROM dbo.DetalleOrdenCompra WHERE ProductoVarianteID=@ProductoVarianteID)
            OR EXISTS (SELECT 1 FROM dbo.DetalleCotizacion WHERE ProductoVarianteID=@ProductoVarianteID)
            OR EXISTS (SELECT 1 FROM dbo.DetalleFactura WHERE ProductoVarianteID=@ProductoVarianteID))
        THROW 52022, 'Una variante utilizada conserva producto, presentacion y color; cree otra variante.', 1;
    IF @ProductoVarianteID=0
    BEGIN
        INSERT dbo.ProductoVariante (ProductoID,PresentacionID,ColorID,CodigoSKU,PrecioVentaActual,StockMinimo,Activo)
        VALUES (@ProductoID,@PresentacionID,@ColorID,@CodigoSKU,@PrecioVentaActual,@StockMinimo,1);
        SET @ProductoVarianteID=SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.ProductoVariante SET ProductoID=@ProductoID, PresentacionID=@PresentacionID, ColorID=@ColorID,
            CodigoSKU=@CodigoSKU, PrecioVentaActual=@PrecioVentaActual, StockMinimo=@StockMinimo WHERE ProductoVarianteID=@ProductoVarianteID;
        IF @@ROWCOUNT=0 THROW 52022, 'Variante inexistente.', 1;
    END;
    SELECT @ProductoVarianteID;
END;
GO
CREATE PROCEDURE dbo.sp_BodegaListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT BodegaID,Codigo,Nombre,Direccion,Activo FROM dbo.Bodega
    WHERE @Buscar IS NULL OR Nombre LIKE '%'+@Buscar+'%' OR Codigo LIKE '%'+@Buscar+'%' ORDER BY Nombre;
END;
GO
CREATE PROCEDURE dbo.sp_BodegaGuardar @UsuarioActorID int, @Codigo varchar(30), @Nombre varchar(100), @Direccion varchar(250)=NULL, @BodegaID int=0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF NULLIF(LTRIM(RTRIM(@Codigo)),'') IS NULL OR NULLIF(LTRIM(RTRIM(@Nombre)),'') IS NULL THROW 52022, 'Codigo y nombre obligatorios.', 1;
    IF @BodegaID=0
    BEGIN
        INSERT dbo.Bodega (Codigo,Nombre,Direccion,Activo) VALUES (@Codigo,@Nombre,@Direccion,1);
        SET @BodegaID=SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Bodega SET Codigo=@Codigo,Nombre=@Nombre,Direccion=@Direccion WHERE BodegaID=@BodegaID;
        IF @@ROWCOUNT=0 THROW 52022, 'Bodega inexistente.', 1;
    END;
    SELECT @BodegaID;
END;
GO
CREATE PROCEDURE dbo.sp_UbicacionListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT u.UbicacionID,u.BodegaID,b.Nombre AS Bodega,u.Codigo,u.Descripcion,u.Activo,b.Activo AS BodegaActiva
    FROM dbo.Ubicacion u JOIN dbo.Bodega b ON b.BodegaID=u.BodegaID
    WHERE @Buscar IS NULL OR u.Codigo LIKE '%'+@Buscar+'%' OR b.Nombre LIKE '%'+@Buscar+'%' ORDER BY b.Nombre,u.Codigo;
END;
GO
CREATE PROCEDURE dbo.sp_UbicacionGuardar @UsuarioActorID int, @BodegaID int, @Codigo varchar(30), @Descripcion varchar(200)=NULL, @UbicacionID int=0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF NULLIF(LTRIM(RTRIM(@Codigo)),'') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Bodega WHERE BodegaID=@BodegaID AND Activo=1)
        THROW 52022, 'Codigo y bodega activa obligatorios.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Ubicacion u JOIN dbo.LoteInventario l ON l.UbicacionID=u.UbicacionID
        WHERE u.UbicacionID=@UbicacionID AND u.BodegaID<>@BodegaID) THROW 52022, 'No se puede trasladar una ubicacion con historia a otra bodega.', 1;
    IF @UbicacionID=0
    BEGIN
        INSERT dbo.Ubicacion (BodegaID,Codigo,Descripcion,Activo) VALUES (@BodegaID,@Codigo,@Descripcion,1);
        SET @UbicacionID=SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Ubicacion SET BodegaID=@BodegaID,Codigo=@Codigo,Descripcion=@Descripcion WHERE UbicacionID=@UbicacionID;
        IF @@ROWCOUNT=0 THROW 52022, 'Ubicacion inexistente.', 1;
    END;
    SELECT @UbicacionID;
END;
GO
CREATE PROCEDURE dbo.sp_ProveedorProductoListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT pp.ProveedorProductoID, pp.ProveedorID, pp.ProductoVarianteID,p.Nombre AS Proveedor,v.CodigoSKU,v.Producto,pp.Activo
    FROM dbo.ProveedorProducto pp JOIN dbo.Proveedor p ON p.ProveedorID=pp.ProveedorID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=pp.ProductoVarianteID
    WHERE @Buscar IS NULL OR p.Nombre LIKE '%'+@Buscar+'%' OR v.CodigoSKU LIKE '%'+@Buscar+'%' ORDER BY p.Nombre,v.CodigoSKU;
END;
GO
CREATE PROCEDURE dbo.sp_ProveedorProductoGuardar @UsuarioActorID int, @ProveedorID int, @ProductoVarianteID int, @ProveedorProductoID int=0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedor WHERE ProveedorID=@ProveedorID AND Activo=1)
       OR NOT EXISTS (SELECT 1 FROM dbo.vw_VariantesDetalle WHERE ProductoVarianteID=@ProductoVarianteID AND Activo=1 AND ProductoActivo=1)
        THROW 52022, 'Seleccione proveedor y variante activos.', 1;
    IF EXISTS (SELECT 1 FROM dbo.ProveedorProducto WHERE ProveedorID=@ProveedorID AND ProductoVarianteID=@ProductoVarianteID AND ProveedorProductoID<>@ProveedorProductoID)
        THROW 52022, 'La asociacion ya existe; puede reactivarla.', 1;
    IF @ProveedorProductoID=0
    BEGIN
        INSERT dbo.ProveedorProducto (ProveedorID,ProductoVarianteID,Activo) VALUES (@ProveedorID,@ProductoVarianteID,1);
        SET @ProveedorProductoID=SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.ProveedorProducto SET ProveedorID=@ProveedorID,ProductoVarianteID=@ProductoVarianteID WHERE ProveedorProductoID=@ProveedorProductoID;
        IF @@ROWCOUNT=0 THROW 52022, 'Asociacion inexistente.', 1;
    END;
    SELECT @ProveedorProductoID;
END;
GO
CREATE PROCEDURE dbo.sp_ColorCrear @UsuarioActorID int, @Nombre varchar(80), @CodigoHex varchar(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF NULLIF(LTRIM(RTRIM(@Nombre)),'') IS NULL THROW 52022, 'Nombre de color obligatorio.', 1;
    IF @CodigoHex IS NOT NULL AND (LEN(@CodigoHex)<>7 OR LEFT(@CodigoHex,1)<>'#' OR SUBSTRING(@CodigoHex,2,6) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-Fa-f]%')
        THROW 52022, 'Color hexadecimal invalido; use #RRGGBB o deje vacio.', 1;
    INSERT dbo.Color (Nombre,CodigoHex,Activo) VALUES (@Nombre,@CodigoHex,1);
    SELECT CONVERT(int,SCOPE_IDENTITY());
END;
GO
CREATE PROCEDURE dbo.sp_MedioPagoListar @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'COBRO';
    SELECT MedioPagoID,Nombre,Activo FROM dbo.MedioPago WHERE Activo=1 AND Nombre IN ('Efectivo','Cheque','Tarjeta') ORDER BY Nombre;
END;
GO
CREATE PROCEDURE dbo.sp_InventarioListar @UsuarioActorID int, @Buscar varchar(180)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    SELECT * FROM dbo.vw_InventarioActual WHERE @Buscar IS NULL OR CodigoSKU LIKE '%'+@Buscar+'%' OR Producto LIKE '%'+@Buscar+'%'
    ORDER BY Producto,FechaIngreso,LoteInventarioID;
END;
GO
CREATE PROCEDURE dbo.sp_ProductoVarianteCambiarActivo @UsuarioActorID int, @ProductoVarianteID int, @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF @Activo IS NULL THROW 52022, 'Estado obligatorio.', 1;
    UPDATE dbo.ProductoVariante SET Activo=@Activo WHERE ProductoVarianteID=@ProductoVarianteID;
    IF @@ROWCOUNT=0 THROW 52022, 'Registro inexistente.', 1;
END;
GO
CREATE PROCEDURE dbo.sp_BodegaCambiarActivo @UsuarioActorID int, @BodegaID int, @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF @Activo IS NULL THROW 52022, 'Estado obligatorio.', 1;
    UPDATE dbo.Bodega SET Activo=@Activo WHERE BodegaID=@BodegaID;
    IF @@ROWCOUNT=0 THROW 52022, 'Registro inexistente.', 1;
END;
GO
CREATE PROCEDURE dbo.sp_UbicacionCambiarActivo @UsuarioActorID int, @UbicacionID int, @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF @Activo IS NULL THROW 52022, 'Estado obligatorio.', 1;
    UPDATE dbo.Ubicacion SET Activo=@Activo WHERE UbicacionID=@UbicacionID;
    IF @@ROWCOUNT=0 THROW 52022, 'Registro inexistente.', 1;
END;
GO
CREATE PROCEDURE dbo.sp_ProveedorProductoCambiarActivo @UsuarioActorID int, @ProveedorProductoID int, @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'INGRESO';
    IF @Activo IS NULL THROW 52022, 'Estado obligatorio.', 1;
    UPDATE dbo.ProveedorProducto SET Activo=@Activo WHERE ProveedorProductoID=@ProveedorProductoID;
    IF @@ROWCOUNT=0 THROW 52022, 'Registro inexistente.', 1;
END;
GO
