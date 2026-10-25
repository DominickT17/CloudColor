USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE VIEW dbo.vw_VentasDetalleNeto
AS
    SELECT f.FacturaID,f.NumeroFactura,f.Fecha,f.Estado,d.DetalleFacturaID,d.ProductoVarianteID,
        v.CodigoSKU,v.Producto,v.Presentacion,v.Color,d.Cantidad AS CantidadOriginal,
        ISNULL(dev.CantidadDevuelta,0) AS CantidadDevuelta,
        d.Cantidad-ISNULL(dev.CantidadDevuelta,0) AS CantidadNeta,
        d.PrecioUnitario,
        CONVERT(decimal(18,2),ROUND((d.Cantidad-ISNULL(dev.CantidadDevuelta,0))*d.PrecioUnitario,2)) AS IngresoNeto
    FROM dbo.Factura f
    JOIN dbo.DetalleFactura d ON d.FacturaID=f.FacturaID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    OUTER APPLY
    (
        SELECT SUM(dd.CantidadDevuelta) AS CantidadDevuelta
        FROM dbo.DetalleFacturaLote fl
        JOIN dbo.DetalleDevolucion dd ON dd.DetalleFacturaLoteID=fl.DetalleFacturaLoteID
        JOIN dbo.Devolucion dev ON dev.DevolucionID=dd.DevolucionID
        WHERE fl.DetalleFacturaID=d.DetalleFacturaID
    ) dev;
GO
