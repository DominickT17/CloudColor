using CloudColor.Entidades;
namespace CloudColor.Web.Models;
public sealed record DocumentoFila(int Id,string Numero,string Persona,DateTime Fecha,string Estado,decimal? Total=null);
public sealed record DocumentosVm(string Ruta,string Titulo,string Descripcion,List<DocumentoFila> Filas,string? Buscar,bool Crear);
public sealed class DocumentoInput
{
    public string Numero {get;set;}="";
    public int PersonaId {get;set;}
    public DateTime? FechaVencimiento {get;set;}
    public string? Observaciones {get;set;}
    public int? CotizacionId {get;set;}
    public List<LineaVenta> Detalle {get;set;}=[];
    public int NuevaVariante {get;set;}
    public decimal NuevaCantidad {get;set;}=1;
    public decimal NuevoPrecio {get;set;}
}
public sealed record EditorDocumentoVm(string Ruta,string Titulo,DocumentoInput Input,List<Opcion> Personas,List<ProductoVariante> Variantes,Dictionary<int,decimal> Disponible);
public sealed record DetalleDocumentoVm(string Ruta,DocumentoFila Documento,List<LineaVenta> Venta,List<LineaCompra> Compra,List<Pago> Pagos,List<Recepcion> Recepciones,string? Emisor=null,string? NIT=null,string? UsuarioEmisor=null,List<LoteFactura>? Lotes=null,decimal? TotalOriginal=null);
public sealed class RecepcionInput
{
    public string Numero {get;set;}="";
    public DateTime FechaIngreso {get;set;}=DateTime.Today;
    public string? Observaciones {get;set;}
    public List<RecibirLinea> Lineas {get;set;}=[];
}
public sealed class RecibirLinea
{
    public bool Incluir {get;set;}
    public int DetalleOrdenCompraID {get;set;}
    public decimal CantidadRecibida {get;set;}
    public decimal CostoUnitario {get;set;}
    public int UbicacionID {get;set;}
    public string? NumeroLoteProveedor {get;set;}
    public DateTime? FechaVencimiento {get;set;}
}
public sealed record RecepcionVm(OrdenCompra Orden,List<LineaCompra> Detalle,List<Ubicacion> Ubicaciones,RecepcionInput Input);
public sealed class CobroInput
{
    public List<Pago> Pagos {get;set;}=[];
    public int NuevoMedio {get;set;}
    public decimal NuevoMonto {get;set;}
    public string? NuevaReferencia {get;set;}
}
public sealed record CobroVm(Factura Factura,List<Pago> Existentes,List<Catalogo> Medios,CobroInput Input,List<LineaVenta> Detalle);
public sealed record RecepcionDetalleVm(OrdenCompra Orden,List<Recepcion> Lineas,List<LineaCompra> Detalle);
public sealed record ReporteProducto(string Producto,string CodigoSKU,string Presentacion,string? Color,decimal Cantidad,decimal Ingresos);
public sealed record ReporteVentasVm(DateTime Desde,DateTime Hasta,decimal TotalFacturado,decimal Efectivo,decimal Cheque,decimal Tarjeta,int CantidadFacturas,string Vista,int? Limite,List<ReporteProducto> Productos,bool Consultado);
public sealed record ReporteSinStockVm(List<VarianteSinStock> Filas);
public sealed class DevolucionInput
{
    public string Numero {get;set;}="";
    public int FacturaID {get;set;}
    public string Motivo {get;set;}="";
    public List<DevolucionLineaInput> Lineas {get;set;}=[];
}
public sealed class DevolucionLineaInput
{
    public bool Incluir {get;set;}
    public int DetalleFacturaLoteID {get;set;}
    public decimal CantidadDevuelta {get;set;}
}
public sealed record DevolucionNuevaVm(List<Factura> Facturas,List<LineaDevolucion> Disponibles,DevolucionInput Input);
public sealed record DevolucionesVm(List<Devolucion> Filas,string? Buscar);
public sealed record DevolucionDetalleVm(Devolucion Devolucion,List<LineaDevolucion> Lineas);
public sealed record InventarioFila(Existencia Lote,decimal TotalVariante,decimal Minimo)
{
    public string Estado=>TotalVariante<=0?"Sin stock":TotalVariante<Minimo?"Stock bajo":"Stock normal";
}
public sealed record InventarioVm(List<InventarioFila> Filas,List<ProductoVariante> Variantes,List<Bodega> Bodegas,string? Buscar,int? Variante,int? Bodega,string? Estado,List<Ubicacion> Ubicaciones,int? Ubicacion,List<ProductoVariante> SinIngresos,int StockBajo,int SinStock);
