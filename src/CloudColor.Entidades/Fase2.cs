namespace CloudColor.Entidades;

public sealed class ProductoVariante
{
    public int ProductoVarianteID { get; set; }
    public int ProductoID { get; set; }
    public int PresentacionID { get; set; }
    public int? ColorID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal PrecioVentaActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }
    public bool ManejaLote { get; set; }
    public bool AplicaPEPS { get; set; }
    public bool ProductoActivo { get; set; }
    public bool PresentacionActiva { get; set; }
    public bool ColorActivo { get; set; }
    public string Etiqueta => $"{CodigoSKU} · {Producto} · {Presentacion} · {Color ?? "Sin color"}";
}
public sealed class Bodega
{
    public int BodegaID { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
}
public sealed class Ubicacion
{
    public int UbicacionID { get; set; }
    public int BodegaID { get; set; }
    public string Bodega { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public bool BodegaActiva { get; set; }
    public string Etiqueta => $"{Bodega} · {Codigo}";
}
public sealed class ProveedorProducto
{
    public int ProveedorProductoID { get; set; }
    public int ProveedorID { get; set; }
    public int ProductoVarianteID { get; set; }
    public string Proveedor { get; set; } = "";
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public bool Activo { get; set; }
}
public sealed class LineaVenta
{
    public int ProductoVarianteID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => decimal.Round(Cantidad*PrecioUnitario,2,MidpointRounding.AwayFromZero);
}
public sealed class Cotizacion
{
    public int CotizacionID { get; set; }
    public string NumeroCotizacion { get; set; } = "";
    public int ClienteID { get; set; }
    public string Cliente { get; set; } = "";
    public DateTime Fecha { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string Estado { get; set; } = "";
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }
}
public sealed class Factura
{
    public int FacturaID { get; set; }
    public string NumeroFactura { get; set; } = "";
    public int ClienteID { get; set; }
    public string Cliente { get; set; } = "";
    public string? NIT { get; set; }
    public int? CotizacionID { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
    public decimal TotalOriginal { get; set; }
    public decimal TotalActual { get; set; }
    public string Emisor { get; set; } = "";
    public string UsuarioEmisor { get; set; } = "";
}
public sealed class Pago
{
    public int MedioPagoID { get; set; }
    public string Medio { get; set; } = "";
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime? FechaHora { get; set; }
    public string Cajero { get; set; } = "";
}
public sealed class OrdenCompra
{
    public int OrdenCompraID { get; set; }
    public string NumeroOrden { get; set; } = "";
    public int ProveedorID { get; set; }
    public string Proveedor { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
    public string? Observaciones { get; set; }
}
public sealed class LineaCompra
{
    public int DetalleOrdenCompraID { get; set; }
    public int ProductoVarianteID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal CostoUnitarioOrden { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CantidadPendiente => CantidadSolicitada-CantidadRecibida;
    public string Etiqueta => $"{CodigoSKU} · pendiente {CantidadPendiente:0.####}";
}
public sealed class LineaRecepcion
{
    public int DetalleOrdenCompraID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public decimal CantidadRecibida { get; set; }
    public decimal CostoUnitario { get; set; }
    public int UbicacionID { get; set; }
    public string Ubicacion { get; set; } = "";
    public string? NumeroLoteProveedor { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
public sealed class Recepcion
{
    public int RecepcionCompraID { get; set; }
    public string NumeroRecepcion { get; set; } = "";
    public int OrdenCompraID { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public string? Observaciones { get; set; }
    public int DetalleOrdenCompraID { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? NumeroLoteProveedor { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string Ubicacion { get; set; } = "";
}
public sealed class Existencia
{
    public int LoteInventarioID { get; set; }
    public int ProductoVarianteID { get; set; }
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string? NumeroLoteProveedor { get; set; }
    public string Bodega { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public int UbicacionID { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal CantidadInicial { get; set; }
    public decimal CantidadDisponible { get; set; }
    public decimal CostoUnitario { get; set; }
}

public sealed class ResumenVentas
{
    public decimal TotalFacturado { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalCheque { get; set; }
    public decimal TotalTarjeta { get; set; }
    public int CantidadFacturas { get; set; }
}

public sealed class ProductoReporte
{
    public int ProductoVarianteID { get; set; }
    public string Producto { get; set; } = "";
    public string CodigoSKU { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal CantidadVendida { get; set; }
    public decimal Ingresos { get; set; }
}

public sealed class VarianteSinStock
{
    public int ProductoVarianteID { get; set; }
    public string Producto { get; set; } = "";
    public string CodigoSKU { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal Disponible { get; set; }
    public decimal StockMinimo { get; set; }
    public string Estado { get; set; } = "";
    public string? Proveedor { get; set; }
}

public sealed class LineaDevolucion
{
    public int DetalleFacturaLoteID { get; set; }
    public int DetalleFacturaID { get; set; }
    public int ProductoVarianteID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CantidadSalida { get; set; }
    public decimal CantidadDevuelta { get; set; }
    public decimal CantidadDisponible { get; set; }
    public string? NumeroLoteProveedor { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string Ubicacion { get; set; } = "";
    public string Bodega { get; set; } = "";
}

public sealed class Devolucion
{
    public int DevolucionID { get; set; }
    public string NumeroDevolucion { get; set; } = "";
    public int FacturaID { get; set; }
    public string NumeroFactura { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = "";
    public string Usuario { get; set; } = "";
    public int Lineas { get; set; }
    public decimal CantidadDevuelta { get; set; }
}

public sealed class LoteFactura
{
    public int DetalleFacturaID { get; set; }
    public string CodigoSKU { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string? Color { get; set; }
    public string? NumeroLoteProveedor { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string Ubicacion { get; set; } = "";
    public string Bodega { get; set; } = "";
    public decimal CantidadSalida { get; set; }
    public decimal CantidadDevuelta { get; set; }
    public decimal CantidadNeta { get; set; }
}
