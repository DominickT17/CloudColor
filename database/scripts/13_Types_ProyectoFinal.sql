-- Tipos de tabla para enviar detalles de devolucion sin concatenar SQL.
USE CloudColorDB;
GO
IF TYPE_ID(N'dbo.TipoDetalleDevolucion') IS NULL
    EXEC(N'CREATE TYPE dbo.TipoDetalleDevolucion AS TABLE
    (
        DetalleFacturaLoteID int NOT NULL PRIMARY KEY,
        CantidadDevuelta decimal(18,4) NOT NULL
    );');
GO
