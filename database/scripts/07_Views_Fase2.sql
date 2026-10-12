USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE VIEW dbo.vw_VariantesDetalle AS
    SELECT v.ProductoVarianteID, v.ProductoID, v.PresentacionID, v.ColorID, v.CodigoSKU,
           p.Nombre AS Producto, pr.Nombre AS Presentacion, c.Nombre AS Color,
           v.PrecioVentaActual, v.StockMinimo, v.Activo, p.ManejaLote, p.AplicaPEPS,
           p.Activo AS ProductoActivo, pr.Activo AS PresentacionActiva,
           CONVERT(bit, CASE WHEN v.ColorID IS NULL THEN 1 ELSE c.Activo END) AS ColorActivo
    FROM dbo.ProductoVariante v JOIN dbo.Producto p ON p.ProductoID=v.ProductoID
    JOIN dbo.Presentacion pr ON pr.PresentacionID=v.PresentacionID
    LEFT JOIN dbo.Color c ON c.ColorID=v.ColorID;
GO
CREATE VIEW dbo.vw_InventarioActual AS
    SELECT l.LoteInventarioID, v.ProductoVarianteID, v.Producto, v.Presentacion, v.Color, v.CodigoSKU,
           l.NumeroLoteProveedor, b.Nombre AS Bodega, u.Codigo AS Ubicacion, l.UbicacionID,
           l.FechaIngreso, l.FechaVencimiento, l.CantidadInicial, l.CantidadDisponible, l.CostoUnitario
    FROM dbo.LoteInventario l
    JOIN dbo.DetalleRecepcionCompra r ON r.DetalleRecepcionCompraID=l.DetalleRecepcionCompraID
    JOIN dbo.DetalleOrdenCompra d ON d.DetalleOrdenCompraID=r.DetalleOrdenCompraID
    JOIN dbo.vw_VariantesDetalle v ON v.ProductoVarianteID=d.ProductoVarianteID
    JOIN dbo.Ubicacion u ON u.UbicacionID=l.UbicacionID JOIN dbo.Bodega b ON b.BodegaID=u.BodegaID;
GO
