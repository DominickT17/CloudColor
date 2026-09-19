-- Ejecutar despues de 00_CreateDatabase.sql, sobre una base sin tablas de usuario.
-- No reemplaza tablas existentes. Si falla, la transaccion revierte toda la estructura.
USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    THROW 50001, 'La base ya contiene tablas. No se modifico su estructura.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE dbo.Rol
    (
        RolID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(50) NOT NULL,
        Descripcion varchar(200) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Rol PRIMARY KEY (RolID),
        CONSTRAINT UQ_Rol_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Empleado
    (
        EmpleadoID int IDENTITY(1,1) NOT NULL,
        CodigoEmpleado varchar(30) NOT NULL,
        Nombres varchar(100) NOT NULL,
        Apellidos varchar(100) NOT NULL,
        Telefono varchar(30) NULL,
        Correo varchar(150) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Empleado PRIMARY KEY (EmpleadoID),
        CONSTRAINT UQ_Empleado_CodigoEmpleado UNIQUE (CodigoEmpleado)
    );

    CREATE TABLE dbo.CategoriaProducto
    (
        CategoriaProductoID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(80) NOT NULL,
        Descripcion varchar(200) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_CategoriaProducto PRIMARY KEY (CategoriaProductoID),
        CONSTRAINT UQ_CategoriaProducto_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Marca
    (
        MarcaID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(100) NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Marca PRIMARY KEY (MarcaID),
        CONSTRAINT UQ_Marca_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Presentacion
    (
        PresentacionID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(80) NOT NULL,
        UnidadMedida varchar(30) NOT NULL,
        Descripcion varchar(200) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Presentacion PRIMARY KEY (PresentacionID),
        CONSTRAINT UQ_Presentacion_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Color
    (
        ColorID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(80) NOT NULL,
        CodigoHex varchar(7) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Color PRIMARY KEY (ColorID),
        CONSTRAINT UQ_Color_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Cliente
    (
        ClienteID int IDENTITY(1,1) NOT NULL,
        TipoCliente varchar(20) NOT NULL,
        NombreRazonSocial varchar(180) NOT NULL,
        NIT varchar(30) NULL,
        Telefono varchar(30) NULL,
        Correo varchar(150) NULL,
        Direccion varchar(250) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Cliente PRIMARY KEY (ClienteID)
    );

    CREATE TABLE dbo.Proveedor
    (
        ProveedorID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(180) NOT NULL,
        NIT varchar(30) NULL,
        Telefono varchar(30) NULL,
        Correo varchar(150) NULL,
        Direccion varchar(250) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Proveedor PRIMARY KEY (ProveedorID)
    );

    CREATE TABLE dbo.Bodega
    (
        BodegaID int IDENTITY(1,1) NOT NULL,
        Codigo varchar(30) NOT NULL,
        Nombre varchar(100) NOT NULL,
        Direccion varchar(250) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Bodega PRIMARY KEY (BodegaID),
        CONSTRAINT UQ_Bodega_Codigo UNIQUE (Codigo)
    );

    CREATE TABLE dbo.MedioPago
    (
        MedioPagoID int IDENTITY(1,1) NOT NULL,
        Nombre varchar(50) NOT NULL,
        Descripcion varchar(200) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_MedioPago PRIMARY KEY (MedioPagoID),
        CONSTRAINT UQ_MedioPago_Nombre UNIQUE (Nombre)
    );

    CREATE TABLE dbo.Usuario
    (
        UsuarioID int IDENTITY(1,1) NOT NULL,
        EmpleadoID int NOT NULL,
        RolID int NOT NULL,
        NombreUsuario varchar(50) NOT NULL,
        PasswordHash varchar(255) NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Usuario PRIMARY KEY (UsuarioID),
        CONSTRAINT UQ_Usuario_EmpleadoID UNIQUE (EmpleadoID),
        CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT FK_Usuario_EmpleadoID FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleado (EmpleadoID),
        CONSTRAINT FK_Usuario_RolID FOREIGN KEY (RolID) REFERENCES dbo.Rol (RolID)
    );

    CREATE TABLE dbo.Producto
    (
        ProductoID int IDENTITY(1,1) NOT NULL,
        CategoriaProductoID int NOT NULL,
        MarcaID int NOT NULL,
        Nombre varchar(150) NOT NULL,
        Descripcion varchar(300) NULL,
        ManejaLote bit NOT NULL,
        AplicaPEPS bit NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Producto PRIMARY KEY (ProductoID),
        CONSTRAINT FK_Producto_CategoriaProductoID FOREIGN KEY (CategoriaProductoID) REFERENCES dbo.CategoriaProducto (CategoriaProductoID),
        CONSTRAINT FK_Producto_MarcaID FOREIGN KEY (MarcaID) REFERENCES dbo.Marca (MarcaID)
    );

    CREATE TABLE dbo.Ubicacion
    (
        UbicacionID int IDENTITY(1,1) NOT NULL,
        BodegaID int NOT NULL,
        Codigo varchar(30) NOT NULL,
        Descripcion varchar(200) NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_Ubicacion PRIMARY KEY (UbicacionID),
        CONSTRAINT UQ_Ubicacion_BodegaID_Codigo UNIQUE (BodegaID, Codigo),
        CONSTRAINT FK_Ubicacion_BodegaID FOREIGN KEY (BodegaID) REFERENCES dbo.Bodega (BodegaID)
    );

    CREATE TABLE dbo.Bitacora
    (
        BitacoraID bigint IDENTITY(1,1) NOT NULL,
        UsuarioID int NOT NULL,
        FechaHora datetime NOT NULL,
        Accion varchar(100) NOT NULL,
        TablaAfectada varchar(100) NOT NULL,
        RegistroID varchar(100) NULL,
        DatosAnteriores varchar(max) NULL,
        DatosNuevos varchar(max) NULL,
        CONSTRAINT PK_Bitacora PRIMARY KEY (BitacoraID),
        CONSTRAINT FK_Bitacora_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.ProductoVariante
    (
        ProductoVarianteID int IDENTITY(1,1) NOT NULL,
        ProductoID int NOT NULL,
        PresentacionID int NOT NULL,
        ColorID int NULL,
        CodigoSKU varchar(50) NOT NULL,
        PrecioVentaActual decimal(18,2) NOT NULL,
        StockMinimo decimal(18,4) NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_ProductoVariante PRIMARY KEY (ProductoVarianteID),
        CONSTRAINT UQ_ProductoVariante_CodigoSKU UNIQUE (CodigoSKU),
        CONSTRAINT UQ_ProductoVariante_ProductoID_PresentacionID_ColorID UNIQUE (ProductoID, PresentacionID, ColorID),
        CONSTRAINT FK_ProductoVariante_ProductoID FOREIGN KEY (ProductoID) REFERENCES dbo.Producto (ProductoID),
        CONSTRAINT FK_ProductoVariante_PresentacionID FOREIGN KEY (PresentacionID) REFERENCES dbo.Presentacion (PresentacionID),
        CONSTRAINT FK_ProductoVariante_ColorID FOREIGN KEY (ColorID) REFERENCES dbo.Color (ColorID),
        CONSTRAINT CK_ProductoVariante_PrecioVentaActual CHECK (PrecioVentaActual >= 0),
        CONSTRAINT CK_ProductoVariante_StockMinimo CHECK (StockMinimo >= 0)
    );

    CREATE TABLE dbo.OrdenCompra
    (
        OrdenCompraID int IDENTITY(1,1) NOT NULL,
        NumeroOrden varchar(30) NOT NULL,
        ProveedorID int NOT NULL,
        UsuarioID int NOT NULL,
        Fecha datetime NOT NULL,
        Estado varchar(30) NOT NULL,
        Observaciones varchar(300) NULL,
        CONSTRAINT PK_OrdenCompra PRIMARY KEY (OrdenCompraID),
        CONSTRAINT UQ_OrdenCompra_NumeroOrden UNIQUE (NumeroOrden),
        CONSTRAINT FK_OrdenCompra_ProveedorID FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedor (ProveedorID),
        CONSTRAINT FK_OrdenCompra_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.Cotizacion
    (
        CotizacionID int IDENTITY(1,1) NOT NULL,
        NumeroCotizacion varchar(30) NOT NULL,
        ClienteID int NOT NULL,
        UsuarioID int NOT NULL,
        Fecha datetime NOT NULL,
        FechaVencimiento date NULL,
        Estado varchar(30) NOT NULL,
        Observaciones varchar(300) NULL,
        CONSTRAINT PK_Cotizacion PRIMARY KEY (CotizacionID),
        CONSTRAINT UQ_Cotizacion_NumeroCotizacion UNIQUE (NumeroCotizacion),
        CONSTRAINT FK_Cotizacion_ClienteID FOREIGN KEY (ClienteID) REFERENCES dbo.Cliente (ClienteID),
        CONSTRAINT FK_Cotizacion_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.ProveedorProducto
    (
        ProveedorProductoID int IDENTITY(1,1) NOT NULL,
        ProveedorID int NOT NULL,
        ProductoVarianteID int NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_ProveedorProducto PRIMARY KEY (ProveedorProductoID),
        CONSTRAINT UQ_ProveedorProducto_ProveedorID_ProductoVarianteID UNIQUE (ProveedorID, ProductoVarianteID),
        CONSTRAINT FK_ProveedorProducto_ProveedorID FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedor (ProveedorID),
        CONSTRAINT FK_ProveedorProducto_ProductoVarianteID FOREIGN KEY (ProductoVarianteID) REFERENCES dbo.ProductoVariante (ProductoVarianteID)
    );

    CREATE TABLE dbo.DetalleOrdenCompra
    (
        DetalleOrdenCompraID int IDENTITY(1,1) NOT NULL,
        OrdenCompraID int NOT NULL,
        ProductoVarianteID int NOT NULL,
        CantidadSolicitada decimal(18,4) NOT NULL,
        CostoUnitarioOrden decimal(18,2) NOT NULL,
        CONSTRAINT PK_DetalleOrdenCompra PRIMARY KEY (DetalleOrdenCompraID),
        CONSTRAINT FK_DetalleOrdenCompra_OrdenCompraID FOREIGN KEY (OrdenCompraID) REFERENCES dbo.OrdenCompra (OrdenCompraID),
        CONSTRAINT FK_DetalleOrdenCompra_ProductoVarianteID FOREIGN KEY (ProductoVarianteID) REFERENCES dbo.ProductoVariante (ProductoVarianteID),
        CONSTRAINT CK_DetalleOrdenCompra_CantidadSolicitada CHECK (CantidadSolicitada > 0),
        CONSTRAINT CK_DetalleOrdenCompra_CostoUnitarioOrden CHECK (CostoUnitarioOrden >= 0)
    );

    CREATE TABLE dbo.RecepcionCompra
    (
        RecepcionCompraID int IDENTITY(1,1) NOT NULL,
        NumeroRecepcion varchar(30) NOT NULL,
        OrdenCompraID int NOT NULL,
        UsuarioID int NOT NULL,
        FechaRecepcion datetime NOT NULL,
        Observaciones varchar(300) NULL,
        CONSTRAINT PK_RecepcionCompra PRIMARY KEY (RecepcionCompraID),
        CONSTRAINT UQ_RecepcionCompra_NumeroRecepcion UNIQUE (NumeroRecepcion),
        CONSTRAINT FK_RecepcionCompra_OrdenCompraID FOREIGN KEY (OrdenCompraID) REFERENCES dbo.OrdenCompra (OrdenCompraID),
        CONSTRAINT FK_RecepcionCompra_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.DetalleCotizacion
    (
        DetalleCotizacionID int IDENTITY(1,1) NOT NULL,
        CotizacionID int NOT NULL,
        ProductoVarianteID int NOT NULL,
        Cantidad decimal(18,4) NOT NULL,
        PrecioUnitario decimal(18,2) NOT NULL,
        CONSTRAINT PK_DetalleCotizacion PRIMARY KEY (DetalleCotizacionID),
        CONSTRAINT FK_DetalleCotizacion_CotizacionID FOREIGN KEY (CotizacionID) REFERENCES dbo.Cotizacion (CotizacionID),
        CONSTRAINT FK_DetalleCotizacion_ProductoVarianteID FOREIGN KEY (ProductoVarianteID) REFERENCES dbo.ProductoVariante (ProductoVarianteID),
        CONSTRAINT CK_DetalleCotizacion_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_DetalleCotizacion_PrecioUnitario CHECK (PrecioUnitario >= 0)
    );

    CREATE TABLE dbo.Factura
    (
        FacturaID int IDENTITY(1,1) NOT NULL,
        NumeroFactura varchar(30) NOT NULL,
        ClienteID int NOT NULL,
        CotizacionID int NULL,
        UsuarioEmisorID int NOT NULL,
        Fecha datetime NOT NULL,
        Estado varchar(30) NOT NULL,
        TotalOriginal decimal(18,2) NOT NULL,
        TotalActual decimal(18,2) NOT NULL,
        CONSTRAINT PK_Factura PRIMARY KEY (FacturaID),
        CONSTRAINT UQ_Factura_NumeroFactura UNIQUE (NumeroFactura),
        CONSTRAINT FK_Factura_ClienteID FOREIGN KEY (ClienteID) REFERENCES dbo.Cliente (ClienteID),
        CONSTRAINT FK_Factura_CotizacionID FOREIGN KEY (CotizacionID) REFERENCES dbo.Cotizacion (CotizacionID),
        CONSTRAINT FK_Factura_UsuarioEmisorID FOREIGN KEY (UsuarioEmisorID) REFERENCES dbo.Usuario (UsuarioID),
        CONSTRAINT CK_Factura_Totales CHECK (TotalOriginal >= 0 AND TotalActual >= 0 AND TotalActual <= TotalOriginal)
    );

    CREATE TABLE dbo.DetalleRecepcionCompra
    (
        DetalleRecepcionCompraID int IDENTITY(1,1) NOT NULL,
        RecepcionCompraID int NOT NULL,
        DetalleOrdenCompraID int NOT NULL,
        CantidadRecibida decimal(18,4) NOT NULL,
        CONSTRAINT PK_DetalleRecepcionCompra PRIMARY KEY (DetalleRecepcionCompraID),
        CONSTRAINT UQ_DetalleRecepcionCompra_RecepcionCompraID_DetalleOrdenCompraID UNIQUE (RecepcionCompraID, DetalleOrdenCompraID),
        CONSTRAINT FK_DetalleRecepcionCompra_RecepcionCompraID FOREIGN KEY (RecepcionCompraID) REFERENCES dbo.RecepcionCompra (RecepcionCompraID),
        CONSTRAINT FK_DetalleRecepcionCompra_DetalleOrdenCompraID FOREIGN KEY (DetalleOrdenCompraID) REFERENCES dbo.DetalleOrdenCompra (DetalleOrdenCompraID),
        CONSTRAINT CK_DetalleRecepcionCompra_CantidadRecibida CHECK (CantidadRecibida > 0)
    );

    CREATE TABLE dbo.DetalleFactura
    (
        DetalleFacturaID int IDENTITY(1,1) NOT NULL,
        FacturaID int NOT NULL,
        ProductoVarianteID int NOT NULL,
        Cantidad decimal(18,4) NOT NULL,
        PrecioUnitario decimal(18,2) NOT NULL,
        CONSTRAINT PK_DetalleFactura PRIMARY KEY (DetalleFacturaID),
        CONSTRAINT FK_DetalleFactura_FacturaID FOREIGN KEY (FacturaID) REFERENCES dbo.Factura (FacturaID),
        CONSTRAINT FK_DetalleFactura_ProductoVarianteID FOREIGN KEY (ProductoVarianteID) REFERENCES dbo.ProductoVariante (ProductoVarianteID),
        CONSTRAINT CK_DetalleFactura_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_DetalleFactura_PrecioUnitario CHECK (PrecioUnitario >= 0)
    );

    CREATE TABLE dbo.PagoFactura
    (
        PagoFacturaID int IDENTITY(1,1) NOT NULL,
        FacturaID int NOT NULL,
        MedioPagoID int NOT NULL,
        UsuarioCajeroID int NOT NULL,
        FechaHora datetime NOT NULL,
        Monto decimal(18,2) NOT NULL,
        Referencia varchar(100) NULL,
        CONSTRAINT PK_PagoFactura PRIMARY KEY (PagoFacturaID),
        CONSTRAINT FK_PagoFactura_FacturaID FOREIGN KEY (FacturaID) REFERENCES dbo.Factura (FacturaID),
        CONSTRAINT FK_PagoFactura_MedioPagoID FOREIGN KEY (MedioPagoID) REFERENCES dbo.MedioPago (MedioPagoID),
        CONSTRAINT FK_PagoFactura_UsuarioCajeroID FOREIGN KEY (UsuarioCajeroID) REFERENCES dbo.Usuario (UsuarioID),
        CONSTRAINT CK_PagoFactura_Monto CHECK (Monto > 0)
    );

    CREATE TABLE dbo.Devolucion
    (
        DevolucionID int IDENTITY(1,1) NOT NULL,
        NumeroDevolucion varchar(30) NOT NULL,
        FacturaID int NOT NULL,
        UsuarioID int NOT NULL,
        FechaHora datetime NOT NULL,
        Motivo varchar(300) NOT NULL,
        CONSTRAINT PK_Devolucion PRIMARY KEY (DevolucionID),
        CONSTRAINT UQ_Devolucion_NumeroDevolucion UNIQUE (NumeroDevolucion),
        CONSTRAINT FK_Devolucion_FacturaID FOREIGN KEY (FacturaID) REFERENCES dbo.Factura (FacturaID),
        CONSTRAINT FK_Devolucion_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.AnulacionFactura
    (
        AnulacionFacturaID int IDENTITY(1,1) NOT NULL,
        FacturaID int NOT NULL,
        UsuarioID int NOT NULL,
        FechaHora datetime NOT NULL,
        Motivo varchar(300) NOT NULL,
        CONSTRAINT PK_AnulacionFactura PRIMARY KEY (AnulacionFacturaID),
        CONSTRAINT UQ_AnulacionFactura_FacturaID UNIQUE (FacturaID),
        CONSTRAINT FK_AnulacionFactura_FacturaID FOREIGN KEY (FacturaID) REFERENCES dbo.Factura (FacturaID),
        CONSTRAINT FK_AnulacionFactura_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID)
    );

    CREATE TABLE dbo.LoteInventario
    (
        LoteInventarioID int IDENTITY(1,1) NOT NULL,
        DetalleRecepcionCompraID int NOT NULL,
        UbicacionID int NOT NULL,
        NumeroLoteProveedor varchar(60) NULL,
        FechaIngreso datetime NOT NULL,
        FechaVencimiento date NULL,
        CantidadInicial decimal(18,4) NOT NULL,
        CantidadDisponible decimal(18,4) NOT NULL,
        CostoUnitario decimal(18,2) NOT NULL,
        CONSTRAINT PK_LoteInventario PRIMARY KEY (LoteInventarioID),
        CONSTRAINT FK_LoteInventario_DetalleRecepcionCompraID FOREIGN KEY (DetalleRecepcionCompraID) REFERENCES dbo.DetalleRecepcionCompra (DetalleRecepcionCompraID),
        CONSTRAINT FK_LoteInventario_UbicacionID FOREIGN KEY (UbicacionID) REFERENCES dbo.Ubicacion (UbicacionID),
        CONSTRAINT CK_LoteInventario_CantidadInicial CHECK (CantidadInicial > 0),
        CONSTRAINT CK_LoteInventario_CantidadDisponible CHECK (CantidadDisponible >= 0 AND CantidadDisponible <= CantidadInicial),
        CONSTRAINT CK_LoteInventario_CostoUnitario CHECK (CostoUnitario >= 0)
    );

    CREATE TABLE dbo.MovimientoInventario
    (
        MovimientoInventarioID bigint IDENTITY(1,1) NOT NULL,
        LoteInventarioID int NOT NULL,
        UsuarioID int NOT NULL,
        FechaHora datetime NOT NULL,
        TipoMovimiento varchar(40) NOT NULL,
        Cantidad decimal(18,4) NOT NULL,
        Referencia varchar(100) NULL,
        Observaciones varchar(300) NULL,
        CONSTRAINT PK_MovimientoInventario PRIMARY KEY (MovimientoInventarioID),
        CONSTRAINT FK_MovimientoInventario_LoteInventarioID FOREIGN KEY (LoteInventarioID) REFERENCES dbo.LoteInventario (LoteInventarioID),
        CONSTRAINT FK_MovimientoInventario_UsuarioID FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuario (UsuarioID),
        CONSTRAINT CK_MovimientoInventario_Cantidad CHECK (Cantidad <> 0)
    );

    CREATE TABLE dbo.DetalleFacturaLote
    (
        DetalleFacturaLoteID int IDENTITY(1,1) NOT NULL,
        DetalleFacturaID int NOT NULL,
        LoteInventarioID int NOT NULL,
        CantidadSalida decimal(18,4) NOT NULL,
        CONSTRAINT PK_DetalleFacturaLote PRIMARY KEY (DetalleFacturaLoteID),
        CONSTRAINT UQ_DetalleFacturaLote_DetalleFacturaID_LoteInventarioID UNIQUE (DetalleFacturaID, LoteInventarioID),
        CONSTRAINT FK_DetalleFacturaLote_DetalleFacturaID FOREIGN KEY (DetalleFacturaID) REFERENCES dbo.DetalleFactura (DetalleFacturaID),
        CONSTRAINT FK_DetalleFacturaLote_LoteInventarioID FOREIGN KEY (LoteInventarioID) REFERENCES dbo.LoteInventario (LoteInventarioID),
        CONSTRAINT CK_DetalleFacturaLote_CantidadSalida CHECK (CantidadSalida > 0)
    );

    CREATE TABLE dbo.DetalleDevolucion
    (
        DetalleDevolucionID int IDENTITY(1,1) NOT NULL,
        DevolucionID int NOT NULL,
        DetalleFacturaLoteID int NOT NULL,
        CantidadDevuelta decimal(18,4) NOT NULL,
        CONSTRAINT PK_DetalleDevolucion PRIMARY KEY (DetalleDevolucionID),
        CONSTRAINT UQ_DetalleDevolucion_DevolucionID_DetalleFacturaLoteID UNIQUE (DevolucionID, DetalleFacturaLoteID),
        CONSTRAINT FK_DetalleDevolucion_DevolucionID FOREIGN KEY (DevolucionID) REFERENCES dbo.Devolucion (DevolucionID),
        CONSTRAINT FK_DetalleDevolucion_DetalleFacturaLoteID FOREIGN KEY (DetalleFacturaLoteID) REFERENCES dbo.DetalleFacturaLote (DetalleFacturaLoteID),
        CONSTRAINT CK_DetalleDevolucion_CantidadDevuelta CHECK (CantidadDevuelta > 0)
    );

    -- Una cotizacion puede originar como maximo una factura.
    -- El filtro permite muchas facturas sin cotizacion previa.
    CREATE UNIQUE INDEX UX_Factura_CotizacionID_NotNull
        ON dbo.Factura (CotizacionID)
        WHERE CotizacionID IS NOT NULL;

    COMMIT TRANSACTION;
    PRINT 'Estructura creada: 31 tablas.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
