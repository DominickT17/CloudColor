-- Instalar una vez despues de 05. Los TVP transportan varias filas con un parametro.
USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE TYPE dbo.TipoDetalleFactura AS TABLE (
    ProductoVarianteID int NOT NULL PRIMARY KEY,
    Cantidad decimal(18,4) NOT NULL,
    PrecioUnitario decimal(18,2) NOT NULL
);
GO
-- Una fila por medio; distribuir el total entre efectivo, cheque y tarjeta.
CREATE TYPE dbo.TipoPagoFactura AS TABLE (
    MedioPagoID int NOT NULL PRIMARY KEY,
    Monto decimal(18,2) NOT NULL,
    Referencia varchar(100) NULL
);
GO
CREATE TYPE dbo.TipoDetalleOrdenCompra AS TABLE (
    ProductoVarianteID int NOT NULL PRIMARY KEY,
    CantidadSolicitada decimal(18,4) NOT NULL,
    CostoUnitarioOrden decimal(18,2) NOT NULL
);
GO
-- Una ubicacion/lote por linea y recepcion. Otra entrega puede usar otra ubicacion.
CREATE TYPE dbo.TipoDetalleRecepcion AS TABLE (
    DetalleOrdenCompraID int NOT NULL PRIMARY KEY,
    CantidadRecibida decimal(18,4) NOT NULL,
    CostoUnitario decimal(18,2) NOT NULL,
    UbicacionID int NOT NULL,
    NumeroLoteProveedor varchar(60) NULL,
    FechaVencimiento date NULL
);
GO
