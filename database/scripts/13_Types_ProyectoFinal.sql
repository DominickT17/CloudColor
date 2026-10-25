-- Tipos de tabla para enviar detalles de devolucion sin concatenar SQL.
USE CloudColorDB;
GO
CREATE TYPE dbo.TipoDetalleDevolucion AS TABLE
(
    DetalleFacturaLoteID int NOT NULL PRIMARY KEY,
    CantidadDevuelta decimal(18,4) NOT NULL
);
GO
