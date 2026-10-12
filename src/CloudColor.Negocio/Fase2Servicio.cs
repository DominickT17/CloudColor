using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;

public sealed class Fase2Servicio(Fase2Datos datos,SeguridadServicio seguridad)
{
    public int ExigirIngreso() => seguridad.ExigirMantenimiento().UsuarioID;
    public int ExigirCobro()
    {
        var s=seguridad.Actual();
        if(s.Rol is not ("Cajero" or "Gerente")) throw new InvalidOperationException("Solo Cajero o Gerente pueden cobrar.");
        return s.UsuarioID;
    }
    public int ExigirConsultaFactura()
    {
        var s=seguridad.Actual();
        if(s.Rol is not ("Digitador" or "Cajero" or "Gerente")) throw new InvalidOperationException("Perfil no habilitado.");
        return s.UsuarioID;
    }
    private static string? Buscar(string? texto) => Validacion.Texto(texto,"Busqueda",180,false);
    private static string Numero(string texto) => Validacion.Texto(texto,"Numero",30,true)!;
    private static string? Observaciones(string? texto) => Validacion.Texto(texto,"Observaciones",300,false);
    private static void Identificador(int id,string nombre) { if(id<=0) throw new ArgumentException("Seleccione "+nombre+"."); }
    private static void Cantidad(decimal valor)
    {
        if(valor<=0 || valor>99999999999999.9999m || decimal.Round(valor,4)!=valor) throw new ArgumentException("Cantidad positiva, con hasta cuatro decimales.");
    }
    private static void Importe(decimal valor)
    {
        if(valor<0 || valor>9999999999999999.99m || decimal.Round(valor,2)!=valor) throw new ArgumentException("Importe no negativo, con hasta dos decimales.");
    }
    private static void Unicos(IEnumerable<int> ids)
    {
        var lista=ids.ToList();
        if(lista.Count==0 || lista.Count!=lista.Distinct().Count()) throw new ArgumentException("Agregue al menos una linea y no repita variantes, lineas ni medios de pago.");
    }
    private static void Venta(List<LineaVenta> lineas)
    {
        Unicos(lineas.Select(l=>l.ProductoVarianteID));
        foreach(var l in lineas) { Identificador(l.ProductoVarianteID,"variante"); Cantidad(l.Cantidad); Importe(l.PrecioUnitario); }
        Importe(Total(lineas));
    }
    public static decimal Total(IEnumerable<LineaVenta> lineas) => lineas.Sum(l=>l.Subtotal);
    public List<ProductoVariante> Variantes(string? buscar=null) => datos.Variantes(ExigirIngreso(),Buscar(buscar));
    public List<Bodega> Bodegas(string? buscar=null) => datos.Bodegas(ExigirIngreso(),Buscar(buscar));
    public List<Ubicacion> Ubicaciones(string? buscar=null) => datos.Ubicaciones(ExigirIngreso(),Buscar(buscar));
    public List<ProveedorProducto> Asociaciones(string? buscar=null) => datos.Asociaciones(ExigirIngreso(),Buscar(buscar));
    public List<Catalogo> Presentaciones() => datos.Presentaciones(ExigirIngreso());
    public List<Catalogo> Colores() => datos.Colores(ExigirIngreso());
    public List<Catalogo> Medios() => datos.Medios(ExigirCobro());
    public List<Existencia> Inventario(string? buscar=null) => datos.Inventario(ExigirIngreso(),Buscar(buscar));
    public int CrearColor(string nombre,string? hex) => datos.CrearColor(ExigirIngreso(),Validacion.Texto(nombre,"Color",80,true)!,Validacion.Texto(hex,"Codigo hexadecimal",7,false));
    public int Guardar(ProductoVariante v)
    {
        int actor=ExigirIngreso();
        Identificador(v.ProductoID,"producto"); Identificador(v.PresentacionID,"presentacion");
        if(v.ColorID.HasValue) Identificador(v.ColorID.Value,"color");
        v.CodigoSKU=Validacion.Texto(v.CodigoSKU,"SKU",50,true)!;
        Importe(v.PrecioVentaActual);
        if(v.StockMinimo<0 || v.StockMinimo>99999999999999.9999m || decimal.Round(v.StockMinimo,4)!=v.StockMinimo) throw new ArgumentException("Stock minimo no negativo, con hasta cuatro decimales.");
        return datos.Guardar(actor,v);
    }
    public int Guardar(Bodega b)
    {
        int actor=ExigirIngreso(); b.Codigo=Validacion.Texto(b.Codigo,"Codigo",30,true)!; b.Nombre=Validacion.Texto(b.Nombre,"Nombre",100,true)!;
        b.Direccion=Validacion.Texto(b.Direccion,"Direccion",250,false); return datos.Guardar(actor,b);
    }
    public int Guardar(Ubicacion u)
    {
        int actor=ExigirIngreso(); Identificador(u.BodegaID,"bodega"); u.Codigo=Validacion.Texto(u.Codigo,"Codigo",30,true)!;
        u.Descripcion=Validacion.Texto(u.Descripcion,"Descripcion",200,false); return datos.Guardar(actor,u);
    }
    public int Guardar(ProveedorProducto p)
    {
        int actor=ExigirIngreso(); Identificador(p.ProveedorID,"proveedor"); Identificador(p.ProductoVarianteID,"variante"); return datos.Guardar(actor,p);
    }
    public void ActivarVariante(int id,bool activo) => datos.ActivarProductoVariante(ExigirIngreso(),id,activo);
    public void ActivarBodega(int id,bool activo) => datos.ActivarBodega(ExigirIngreso(),id,activo);
    public void ActivarUbicacion(int id,bool activo) => datos.ActivarUbicacion(ExigirIngreso(),id,activo);
    public void ActivarAsociacion(int id,bool activo) => datos.ActivarProveedorProducto(ExigirIngreso(),id,activo);
    public List<Cotizacion> Cotizaciones(string? buscar=null) => datos.Cotizaciones(ExigirIngreso(),Buscar(buscar));
    public List<LineaVenta> CotizacionDetalle(int id) => datos.CotizacionDetalle(ExigirIngreso(),id);
    public int CrearCotizacion(string numero,int cliente,List<LineaVenta> lineas,DateTime? vence=null,string? observaciones=null)
    {
        int actor=ExigirIngreso(); Identificador(cliente,"cliente"); Venta(lineas);
        if(vence?.Date<DateTime.Today) throw new ArgumentException("La cotizacion no puede vencer antes de hoy.");
        return datos.CrearCotizacion(actor,Numero(numero),cliente,lineas,vence,Observaciones(observaciones));
    }
    public void Confirmar(int id) => datos.Confirmar(ExigirIngreso(),id);
    public List<Factura> Facturas(string? buscar=null) => datos.Facturas(ExigirConsultaFactura(),Buscar(buscar));
    public List<LineaVenta> FacturaDetalle(int id) => datos.FacturaDetalle(ExigirConsultaFactura(),id);
    public List<Pago> FacturaPagos(int id) => datos.FacturaPagos(ExigirConsultaFactura(),id);
    public int Facturar(string numero,int cliente,List<LineaVenta> lineas,int? cotizacion=null)
    {
        int actor=ExigirIngreso(); Identificador(cliente,"cliente");
        if(cotizacion.HasValue)
        {
            Identificador(cotizacion.Value,"cotizacion");
            if(lineas.Count!=0) throw new ArgumentException("La cotizacion proporciona el detalle historico completo.");
        }
        else Venta(lineas);
        return datos.Facturar(actor,Numero(numero),cliente,lineas,cotizacion);
    }
    public void Cobrar(int factura,List<Pago> pagos)
    {
        int actor=ExigirCobro(); Identificador(factura,"factura"); Unicos(pagos.Select(p=>p.MedioPagoID));
        foreach(var p in pagos)
        {
            Identificador(p.MedioPagoID,"medio de pago"); Importe(p.Monto);
            if(p.Monto<=0) throw new ArgumentException("Los pagos deben ser positivos.");
            p.Referencia=Validacion.Texto(p.Referencia,"Referencia",100,false);
        }
        datos.Cobrar(actor,factura,pagos);
    }
    public List<OrdenCompra> Ordenes(string? buscar=null) => datos.Ordenes(ExigirIngreso(),Buscar(buscar));
    public List<LineaCompra> OrdenDetalle(int id) => datos.OrdenDetalle(ExigirIngreso(),id);
    public List<Recepcion> Recepciones(int id) => datos.Recepciones(ExigirIngreso(),id);
    public int CrearOrden(string numero,int proveedor,List<LineaCompra> lineas,string? observaciones=null)
    {
        int actor=ExigirIngreso(); Identificador(proveedor,"proveedor"); Unicos(lineas.Select(l=>l.ProductoVarianteID));
        foreach(var l in lineas) { Identificador(l.ProductoVarianteID,"variante"); Cantidad(l.CantidadSolicitada); Importe(l.CostoUnitarioOrden); }
        return datos.CrearOrden(actor,Numero(numero),proveedor,lineas,Observaciones(observaciones));
    }
    public int Recibir(string numero,int orden,DateTime fecha,List<LineaRecepcion> lineas,string? observaciones=null)
    {
        int actor=ExigirIngreso(); Identificador(orden,"orden"); Unicos(lineas.Select(l=>l.DetalleOrdenCompraID));
        if(fecha>DateTime.Now || fecha.Year<1753) throw new ArgumentException("Fecha de ingreso invalida o futura.");
        foreach(var l in lineas)
        {
            Identificador(l.DetalleOrdenCompraID,"linea de orden"); Identificador(l.UbicacionID,"ubicacion"); Cantidad(l.CantidadRecibida); Importe(l.CostoUnitario);
            l.NumeroLoteProveedor=Validacion.Texto(l.NumeroLoteProveedor,"Lote proveedor",60,false);
            if(l.FechaVencimiento?.Date<fecha.Date) throw new ArgumentException("El vencimiento no puede ser anterior al ingreso.");
        }
        return datos.Recibir(actor,Numero(numero),orden,fecha,lineas,Observaciones(observaciones));
    }
}
