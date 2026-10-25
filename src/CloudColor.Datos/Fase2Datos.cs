using System.Data;
using CloudColor.Entidades;
using Microsoft.Data.SqlClient;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;

public sealed class Fase2Datos(AccesoDatos db)
{
    private static SqlParameter ID(string nombre,int? valor) => new(nombre,SqlDbType.Int) { Value=(object?)valor ?? DBNull.Value };
    private static SqlParameter Decimal(string nombre,decimal valor,byte escala=2) => new(nombre,SqlDbType.Decimal) { Precision=18,Scale=escala,Value=valor };
    private static SqlParameter Fecha(string nombre,DateTime? valor,bool soloFecha=false) => new(nombre,soloFecha ? SqlDbType.Date : SqlDbType.DateTime) { Value=(object?)valor ?? DBNull.Value };
    private static SqlParameter Tabla(string nombre,string tipo,DataTable datos) => new(nombre,SqlDbType.Structured) { TypeName="dbo."+tipo,Value=datos };
    private static ProductoVariante LeerProductoVariante(SqlDataReader r) => new()
    {
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        ProductoID=(int)r["ProductoID"],
        PresentacionID=(int)r["PresentacionID"],
        ColorID=r["ColorID"]==DBNull.Value ? null : (int)r["ColorID"],
        CodigoSKU=(string)r["CodigoSKU"],
        Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],
        Color=r["Color"]==DBNull.Value ? null : (string)r["Color"],
        PrecioVentaActual=(decimal)r["PrecioVentaActual"],
        StockMinimo=(decimal)r["StockMinimo"],
        Activo=(bool)r["Activo"],
        ManejaLote=(bool)r["ManejaLote"],
        AplicaPEPS=(bool)r["AplicaPEPS"],
        ProductoActivo=(bool)r["ProductoActivo"],
        PresentacionActiva=(bool)r["PresentacionActiva"],
        ColorActivo=(bool)r["ColorActivo"],
    };
    private static Bodega LeerBodega(SqlDataReader r) => new()
    {
        BodegaID=(int)r["BodegaID"],
        Codigo=(string)r["Codigo"],
        Nombre=(string)r["Nombre"],
        Direccion=r["Direccion"]==DBNull.Value ? null : (string)r["Direccion"],
        Activo=(bool)r["Activo"],
    };
    private static Ubicacion LeerUbicacion(SqlDataReader r) => new()
    {
        UbicacionID=(int)r["UbicacionID"],
        BodegaID=(int)r["BodegaID"],
        Bodega=(string)r["Bodega"],
        Codigo=(string)r["Codigo"],
        Descripcion=r["Descripcion"]==DBNull.Value ? null : (string)r["Descripcion"],
        Activo=(bool)r["Activo"],
        BodegaActiva=(bool)r["BodegaActiva"],
    };
    private static ProveedorProducto LeerProveedorProducto(SqlDataReader r) => new()
    {
        ProveedorProductoID=(int)r["ProveedorProductoID"],
        ProveedorID=(int)r["ProveedorID"],
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        Proveedor=(string)r["Proveedor"],
        CodigoSKU=(string)r["CodigoSKU"],
        Producto=(string)r["Producto"],
        Activo=(bool)r["Activo"],
    };
    private static LineaVenta LeerLineaVenta(SqlDataReader r) => new()
    {
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        CodigoSKU=(string)r["CodigoSKU"],
        Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],
        Color=r["Color"]==DBNull.Value ? null : (string)r["Color"],
        Cantidad=(decimal)r["Cantidad"],
        PrecioUnitario=(decimal)r["PrecioUnitario"],
    };
    private static Cotizacion LeerCotizacion(SqlDataReader r) => new()
    {
        CotizacionID=(int)r["CotizacionID"],
        NumeroCotizacion=(string)r["NumeroCotizacion"],
        ClienteID=(int)r["ClienteID"],
        Cliente=(string)r["Cliente"],
        Fecha=(DateTime)r["Fecha"],
        FechaVencimiento=r["FechaVencimiento"]==DBNull.Value ? null : (DateTime)r["FechaVencimiento"],
        Estado=(string)r["Estado"],
        Observaciones=r["Observaciones"]==DBNull.Value ? null : (string)r["Observaciones"],
        Total=(decimal)r["Total"],
    };
    private static Factura LeerFactura(SqlDataReader r) => new()
    {
        FacturaID=(int)r["FacturaID"],
        NumeroFactura=(string)r["NumeroFactura"],
        ClienteID=(int)r["ClienteID"],
        Cliente=(string)r["Cliente"],
        NIT=r["NIT"]==DBNull.Value ? null : (string)r["NIT"],
        CotizacionID=r["CotizacionID"]==DBNull.Value ? null : (int)r["CotizacionID"],
        Fecha=(DateTime)r["Fecha"],
        Estado=(string)r["Estado"],
        TotalOriginal=(decimal)r["TotalOriginal"],
        TotalActual=(decimal)r["TotalActual"],
        Emisor=(string)r["Emisor"],
        UsuarioEmisor=(string)r["UsuarioEmisor"],
    };
    private static Pago LeerPago(SqlDataReader r) => new()
    {
        MedioPagoID=(int)r["MedioPagoID"],
        Medio=(string)r["Medio"],
        Monto=(decimal)r["Monto"],
        Referencia=r["Referencia"]==DBNull.Value ? null : (string)r["Referencia"],
        FechaHora=r["FechaHora"]==DBNull.Value ? null : (DateTime)r["FechaHora"],
        Cajero=(string)r["Cajero"],
    };
    private static OrdenCompra LeerOrdenCompra(SqlDataReader r) => new()
    {
        OrdenCompraID=(int)r["OrdenCompraID"],
        NumeroOrden=(string)r["NumeroOrden"],
        ProveedorID=(int)r["ProveedorID"],
        Proveedor=(string)r["Proveedor"],
        Fecha=(DateTime)r["Fecha"],
        Estado=(string)r["Estado"],
        Observaciones=r["Observaciones"]==DBNull.Value ? null : (string)r["Observaciones"],
    };
    private static LineaCompra LeerLineaCompra(SqlDataReader r) => new()
    {
        DetalleOrdenCompraID=(int)r["DetalleOrdenCompraID"],
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        CodigoSKU=(string)r["CodigoSKU"],
        Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],
        Color=r["Color"]==DBNull.Value ? null : (string)r["Color"],
        CantidadSolicitada=(decimal)r["CantidadSolicitada"],
        CostoUnitarioOrden=(decimal)r["CostoUnitarioOrden"],
        CantidadRecibida=(decimal)r["CantidadRecibida"],
    };
    private static LineaRecepcion LeerLineaRecepcion(SqlDataReader r) => new()
    {
        DetalleOrdenCompraID=(int)r["DetalleOrdenCompraID"],
        CodigoSKU=(string)r["CodigoSKU"],
        CantidadRecibida=(decimal)r["CantidadRecibida"],
        CostoUnitario=(decimal)r["CostoUnitario"],
        UbicacionID=(int)r["UbicacionID"],
        Ubicacion=(string)r["Ubicacion"],
        NumeroLoteProveedor=r["NumeroLoteProveedor"]==DBNull.Value ? null : (string)r["NumeroLoteProveedor"],
        FechaVencimiento=r["FechaVencimiento"]==DBNull.Value ? null : (DateTime)r["FechaVencimiento"],
    };
    private static Recepcion LeerRecepcion(SqlDataReader r) => new()
    {
        RecepcionCompraID=(int)r["RecepcionCompraID"],
        NumeroRecepcion=(string)r["NumeroRecepcion"],
        OrdenCompraID=(int)r["OrdenCompraID"],
        FechaRecepcion=(DateTime)r["FechaRecepcion"],
        Observaciones=r["Observaciones"]==DBNull.Value ? null : (string)r["Observaciones"],
        DetalleOrdenCompraID=(int)r["DetalleOrdenCompraID"],
        CantidadRecibida=(decimal)r["CantidadRecibida"],
        CostoUnitario=(decimal)r["CostoUnitario"],
        NumeroLoteProveedor=r["NumeroLoteProveedor"]==DBNull.Value ? null : (string)r["NumeroLoteProveedor"],
        FechaIngreso=(DateTime)r["FechaIngreso"],
        Ubicacion=(string)r["Ubicacion"],
    };
    private static Existencia LeerExistencia(SqlDataReader r) => new()
    {
        LoteInventarioID=(int)r["LoteInventarioID"],
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],
        Color=r["Color"]==DBNull.Value ? null : (string)r["Color"],
        CodigoSKU=(string)r["CodigoSKU"],
        NumeroLoteProveedor=r["NumeroLoteProveedor"]==DBNull.Value ? null : (string)r["NumeroLoteProveedor"],
        Bodega=(string)r["Bodega"],
        Ubicacion=(string)r["Ubicacion"],
        UbicacionID=(int)r["UbicacionID"],
        FechaIngreso=(DateTime)r["FechaIngreso"],
        FechaVencimiento=r["FechaVencimiento"]==DBNull.Value ? null : (DateTime)r["FechaVencimiento"],
        CantidadInicial=(decimal)r["CantidadInicial"],
        CantidadDisponible=(decimal)r["CantidadDisponible"],
        CostoUnitario=(decimal)r["CostoUnitario"],
    };
    private static LineaDevolucion LeerLineaDevolucion(SqlDataReader r) => new()
    {
        DetalleFacturaLoteID=(int)r["DetalleFacturaLoteID"],
        DetalleFacturaID=(int)r["DetalleFacturaID"],
        ProductoVarianteID=(int)r["ProductoVarianteID"],
        CodigoSKU=(string)r["CodigoSKU"],Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],Color=r["Color"]==DBNull.Value?null:(string)r["Color"],
        PrecioUnitario=(decimal)r["PrecioUnitario"],CantidadSalida=(decimal)r["CantidadSalida"],
        CantidadDevuelta=(decimal)r["CantidadDevuelta"],CantidadDisponible=(decimal)r["CantidadDisponible"],
        NumeroLoteProveedor=r["NumeroLoteProveedor"]==DBNull.Value?null:(string)r["NumeroLoteProveedor"],
        FechaIngreso=(DateTime)r["FechaIngreso"],Ubicacion=(string)r["Ubicacion"],Bodega=(string)r["Bodega"],
    };
    private static ProductoReporte LeerProductoReporte(SqlDataReader r) => new()
    {
        ProductoVarianteID=(int)r["ProductoVarianteID"],Producto=(string)r["Producto"],
        CodigoSKU=(string)r["CodigoSKU"],Presentacion=(string)r["Presentacion"],
        Color=r["Color"]==DBNull.Value?null:(string)r["Color"],CantidadVendida=(decimal)r["CantidadVendida"],
        Ingresos=(decimal)r["Ingresos"],
    };
    private static VarianteSinStock LeerVarianteSinStock(SqlDataReader r) => new()
    {
        ProductoVarianteID=(int)r["ProductoVarianteID"],Producto=(string)r["Producto"],CodigoSKU=(string)r["CodigoSKU"],
        Presentacion=(string)r["Presentacion"],Color=r["Color"]==DBNull.Value?null:(string)r["Color"],
        Disponible=(decimal)r["Disponible"],StockMinimo=(decimal)r["StockMinimo"],Estado=(string)r["Estado"],
        Proveedor=r["Proveedor"]==DBNull.Value?null:(string)r["Proveedor"],
    };
    private static Devolucion LeerDevolucion(SqlDataReader r) => new()
    {
        DevolucionID=(int)r["DevolucionID"],NumeroDevolucion=(string)r["NumeroDevolucion"],
        FacturaID=(int)r["FacturaID"],NumeroFactura=(string)r["NumeroFactura"],FechaHora=(DateTime)r["FechaHora"],
        Motivo=(string)r["Motivo"],Usuario=(string)r["Usuario"],Lineas=(int)r["Lineas"],
        CantidadDevuelta=(decimal)r["CantidadDevuelta"],
    };
    private static LoteFactura LeerLoteFactura(SqlDataReader r) => new()
    {
        DetalleFacturaID=(int)r["DetalleFacturaID"],CodigoSKU=(string)r["CodigoSKU"],Producto=(string)r["Producto"],
        Presentacion=(string)r["Presentacion"],Color=r["Color"]==DBNull.Value?null:(string)r["Color"],
        NumeroLoteProveedor=r["NumeroLoteProveedor"]==DBNull.Value?null:(string)r["NumeroLoteProveedor"],
        FechaIngreso=(DateTime)r["FechaIngreso"],Ubicacion=(string)r["Ubicacion"],Bodega=(string)r["Bodega"],
        CantidadSalida=(decimal)r["CantidadSalida"],CantidadDevuelta=(decimal)r["CantidadDevuelta"],
        CantidadNeta=(decimal)r["CantidadNeta"],
    };
    public List<ProductoVariante> Variantes(int actor,string? buscar) => db.Consultar("dbo.sp_ProductoVarianteListar",LeerProductoVariante,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<Bodega> Bodegas(int actor,string? buscar) => db.Consultar("dbo.sp_BodegaListar",LeerBodega,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<Ubicacion> Ubicaciones(int actor,string? buscar) => db.Consultar("dbo.sp_UbicacionListar",LeerUbicacion,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<ProveedorProducto> Asociaciones(int actor,string? buscar) => db.Consultar("dbo.sp_ProveedorProductoListar",LeerProveedorProducto,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<Cotizacion> Cotizaciones(int actor,string? buscar) => db.Consultar("dbo.sp_CotizacionListar",LeerCotizacion,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<Factura> Facturas(int actor,string? buscar) => db.Consultar("dbo.sp_FacturaListar",LeerFactura,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<OrdenCompra> Ordenes(int actor,string? buscar) => db.Consultar("dbo.sp_OrdenCompraListar",LeerOrdenCompra,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<Existencia> Inventario(int actor,string? buscar) => db.Consultar("dbo.sp_InventarioListar",LeerExistencia,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<LoteFactura> LotesFactura(int actor,int factura) => db.Consultar("dbo.sp_FacturaLotes",LeerLoteFactura,Entero("@UsuarioActorID",actor),Entero("@FacturaID",factura));
    public ResumenVentas ReporteVentas(int actor,DateTime desde,DateTime hasta) => db.Consultar("dbo.sp_ReporteVentas",r=>new ResumenVentas
    {
        TotalFacturado=(decimal)r["TotalFacturado"],TotalEfectivo=(decimal)r["TotalEfectivo"],TotalCheque=(decimal)r["TotalCheque"],
        TotalTarjeta=(decimal)r["TotalTarjeta"],CantidadFacturas=(int)r["CantidadFacturas"],
    },Entero("@UsuarioActorID",actor),Fecha("@FechaInicio",desde,true),Fecha("@FechaFin",hasta,true)).Single();
    public List<ProductoReporte> ReporteProductos(int actor,DateTime desde,DateTime hasta,string tipo,int? limite) => db.Consultar("dbo.sp_ReporteProductos",LeerProductoReporte,
        Entero("@UsuarioActorID",actor),Fecha("@FechaInicio",desde,true),Fecha("@FechaFin",hasta,true),Texto("@Tipo",20,tipo),ID("@Limite",limite));
    public List<VarianteSinStock> ReporteSinStock(int actor) => db.Consultar("dbo.sp_ReporteSinStock",LeerVarianteSinStock,Entero("@UsuarioActorID",actor));
    public List<LineaDevolucion> DevolucionDisponibles(int actor,int factura) => db.Consultar("dbo.sp_DevolucionDisponibles",LeerLineaDevolucion,Entero("@UsuarioActorID",actor),Entero("@FacturaID",factura));
    public List<Devolucion> Devoluciones(int actor,string? buscar) => db.Consultar("dbo.sp_DevolucionListar",LeerDevolucion,Entero("@UsuarioActorID",actor),Texto("@Buscar",180,buscar));
    public List<LineaDevolucion> DevolucionDetalle(int actor,int devolucion) => db.Consultar("dbo.sp_DevolucionDetalle",LeerLineaDevolucion,Entero("@UsuarioActorID",actor),Entero("@DevolucionID",devolucion));
    public int CrearDevolucion(int actor,string numero,int factura,string motivo,List<LineaDevolucion> lineas) =>
        db.Escalar("dbo.sp_DevolucionCrear",Entero("@UsuarioActorID",actor),Texto("@NumeroDevolucion",30,numero),Entero("@FacturaID",factura),Texto("@Motivo",300,motivo),Tabla("@Detalles","TipoDetalleDevolucion",DetalleDevolucion(lineas)));
    public void AnularFactura(int actor,int factura,string motivo) => db.Ejecutar("dbo.sp_FacturaAnular",Entero("@UsuarioActorID",actor),Entero("@FacturaID",factura),Texto("@Motivo",300,motivo));
    public List<LineaVenta> CotizacionDetalle(int actor,int id) => db.Consultar("dbo.sp_CotizacionDetalle",LeerLineaVenta,Entero("@UsuarioActorID",actor),Entero("@CotizacionID",id));
    public List<LineaVenta> FacturaDetalle(int actor,int id) => db.Consultar("dbo.sp_FacturaDetalle",LeerLineaVenta,Entero("@UsuarioActorID",actor),Entero("@FacturaID",id));
    public List<Pago> FacturaPagos(int actor,int id) => db.Consultar("dbo.sp_FacturaPagos",LeerPago,Entero("@UsuarioActorID",actor),Entero("@FacturaID",id));
    public List<LineaCompra> OrdenDetalle(int actor,int id) => db.Consultar("dbo.sp_OrdenCompraDetalle",LeerLineaCompra,Entero("@UsuarioActorID",actor),Entero("@OrdenCompraID",id));
    public List<Recepcion> Recepciones(int actor,int id) => db.Consultar("dbo.sp_RecepcionCompraListar",LeerRecepcion,Entero("@UsuarioActorID",actor),Entero("@OrdenCompraID",id));
    public List<Catalogo> Presentaciones(int actor) => db.Consultar("dbo.sp_PresentacionListar",r=>new Catalogo((int)r["PresentacionID"],(string)r["Nombre"],(bool)r["Activo"]),Entero("@UsuarioActorID",actor));
    public List<Catalogo> Colores(int actor) => db.Consultar("dbo.sp_ColorListar",r=>new Catalogo((int)r["ColorID"],(string)r["Nombre"],(bool)r["Activo"]),Entero("@UsuarioActorID",actor));
    public List<Catalogo> Medios(int actor) => db.Consultar("dbo.sp_MedioPagoListar",r=>new Catalogo((int)r["MedioPagoID"],(string)r["Nombre"],(bool)r["Activo"]),Entero("@UsuarioActorID",actor));
    public int CrearColor(int actor,string nombre,string? hex) => db.Escalar("dbo.sp_ColorCrear",Entero("@UsuarioActorID",actor),Texto("@Nombre",80,nombre),Texto("@CodigoHex",7,hex));
    public int Guardar(int actor,ProductoVariante v) => db.Escalar("dbo.sp_ProductoVarianteGuardar",Entero("@UsuarioActorID",actor),Entero("@ProductoVarianteID",v.ProductoVarianteID),Entero("@ProductoID",v.ProductoID),Entero("@PresentacionID",v.PresentacionID),ID("@ColorID",v.ColorID),Texto("@CodigoSKU",50,v.CodigoSKU),Decimal("@PrecioVentaActual",v.PrecioVentaActual),Decimal("@StockMinimo",v.StockMinimo,4));
    public int Guardar(int actor,Bodega b) => db.Escalar("dbo.sp_BodegaGuardar",Entero("@UsuarioActorID",actor),Entero("@BodegaID",b.BodegaID),Texto("@Codigo",30,b.Codigo),Texto("@Nombre",100,b.Nombre),Texto("@Direccion",250,b.Direccion));
    public int Guardar(int actor,Ubicacion u) => db.Escalar("dbo.sp_UbicacionGuardar",Entero("@UsuarioActorID",actor),Entero("@UbicacionID",u.UbicacionID),Entero("@BodegaID",u.BodegaID),Texto("@Codigo",30,u.Codigo),Texto("@Descripcion",200,u.Descripcion));
    public int Guardar(int actor,ProveedorProducto p) => db.Escalar("dbo.sp_ProveedorProductoGuardar",Entero("@UsuarioActorID",actor),Entero("@ProveedorProductoID",p.ProveedorProductoID),Entero("@ProveedorID",p.ProveedorID),Entero("@ProductoVarianteID",p.ProductoVarianteID));
    public void ActivarProductoVariante(int actor,int id,bool activo) => db.Ejecutar("dbo.sp_ProductoVarianteCambiarActivo",Entero("@UsuarioActorID",actor),Entero("@ProductoVarianteID",id),Bit("@Activo",activo));
    public void ActivarBodega(int actor,int id,bool activo) => db.Ejecutar("dbo.sp_BodegaCambiarActivo",Entero("@UsuarioActorID",actor),Entero("@BodegaID",id),Bit("@Activo",activo));
    public void ActivarUbicacion(int actor,int id,bool activo) => db.Ejecutar("dbo.sp_UbicacionCambiarActivo",Entero("@UsuarioActorID",actor),Entero("@UbicacionID",id),Bit("@Activo",activo));
    public void ActivarProveedorProducto(int actor,int id,bool activo) => db.Ejecutar("dbo.sp_ProveedorProductoCambiarActivo",Entero("@UsuarioActorID",actor),Entero("@ProveedorProductoID",id),Bit("@Activo",activo));
    public int CrearCotizacion(int actor,string numero,int cliente,List<LineaVenta> lineas,DateTime? vence,string? observaciones) => db.Escalar("dbo.sp_CotizacionCrear",Entero("@UsuarioActorID",actor),Texto("@NumeroCotizacion",30,numero),Entero("@ClienteID",cliente),Tabla("@Detalles","TipoDetalleFactura",DetalleVenta(lineas)),Fecha("@FechaVencimiento",vence,true),Texto("@Observaciones",300,observaciones));
    public void Confirmar(int actor,int id) => db.Ejecutar("dbo.sp_CotizacionConfirmar",Entero("@UsuarioActorID",actor),Entero("@CotizacionID",id));
    public int Facturar(int actor,string numero,int cliente,List<LineaVenta> lineas,int? cotizacion) => db.Escalar("dbo.sp_FacturaGenerar",Entero("@UsuarioActorID",actor),Texto("@NumeroFactura",30,numero),Entero("@ClienteID",cliente),Tabla("@Detalles","TipoDetalleFactura",DetalleVenta(lineas)),ID("@CotizacionID",cotizacion));
    public void Cobrar(int actor,int factura,List<Pago> pagos) => db.Ejecutar("dbo.sp_FacturaCobrar",Entero("@UsuarioActorID",actor),Entero("@FacturaID",factura),Tabla("@Pagos","TipoPagoFactura",DetallePago(pagos)));
    public int CrearOrden(int actor,string numero,int proveedor,List<LineaCompra> lineas,string? observaciones) => db.Escalar("dbo.sp_OrdenCompraCrear",Entero("@UsuarioActorID",actor),Texto("@NumeroOrden",30,numero),Entero("@ProveedorID",proveedor),Tabla("@Detalles","TipoDetalleOrdenCompra",DetalleCompra(lineas)),Texto("@Observaciones",300,observaciones));
    public int Recibir(int actor,string numero,int orden,DateTime fecha,List<LineaRecepcion> lineas,string? observaciones) => db.Escalar("dbo.sp_RecepcionCompraRegistrar",Entero("@UsuarioActorID",actor),Texto("@NumeroRecepcion",30,numero),Entero("@OrdenCompraID",orden),Fecha("@FechaIngreso",fecha),Tabla("@Detalles","TipoDetalleRecepcion",DetalleRecepcion(lineas)),Texto("@Observaciones",300,observaciones));
    private static DataTable DetalleVenta(IEnumerable<LineaVenta> lineas)
    {
        var tabla=new DataTable();
        tabla.Columns.Add("ProductoVarianteID",typeof(int));
        tabla.Columns.Add("Cantidad",typeof(decimal));
        tabla.Columns.Add("PrecioUnitario",typeof(decimal));
        foreach(var l in lineas) tabla.Rows.Add(l.ProductoVarianteID,l.Cantidad,l.PrecioUnitario);
        return tabla;
    }
    private static DataTable DetallePago(IEnumerable<Pago> lineas)
    {
        var tabla=new DataTable();
        tabla.Columns.Add("MedioPagoID",typeof(int));
        tabla.Columns.Add("Monto",typeof(decimal));
        tabla.Columns.Add("Referencia",typeof(string));
        foreach(var l in lineas) tabla.Rows.Add(l.MedioPagoID,l.Monto,(object?)l.Referencia ?? DBNull.Value);
        return tabla;
    }
    private static DataTable DetalleCompra(IEnumerable<LineaCompra> lineas)
    {
        var tabla=new DataTable();
        tabla.Columns.Add("ProductoVarianteID",typeof(int));
        tabla.Columns.Add("CantidadSolicitada",typeof(decimal));
        tabla.Columns.Add("CostoUnitarioOrden",typeof(decimal));
        foreach(var l in lineas) tabla.Rows.Add(l.ProductoVarianteID,l.CantidadSolicitada,l.CostoUnitarioOrden);
        return tabla;
    }
    private static DataTable DetalleRecepcion(IEnumerable<LineaRecepcion> lineas)
    {
        var tabla=new DataTable();
        tabla.Columns.Add("DetalleOrdenCompraID",typeof(int));
        tabla.Columns.Add("CantidadRecibida",typeof(decimal));
        tabla.Columns.Add("CostoUnitario",typeof(decimal));
        tabla.Columns.Add("UbicacionID",typeof(int));
        tabla.Columns.Add("NumeroLoteProveedor",typeof(string));
        tabla.Columns.Add("FechaVencimiento",typeof(DateTime));
        foreach(var l in lineas) tabla.Rows.Add(l.DetalleOrdenCompraID,l.CantidadRecibida,l.CostoUnitario,l.UbicacionID,(object?)l.NumeroLoteProveedor ?? DBNull.Value,(object?)l.FechaVencimiento ?? DBNull.Value);
        return tabla;
    }
    private static DataTable DetalleDevolucion(IEnumerable<LineaDevolucion> lineas)
    {
        var tabla=new DataTable();tabla.Columns.Add("DetalleFacturaLoteID",typeof(int));tabla.Columns.Add("CantidadDevuelta",typeof(decimal));
        foreach(var l in lineas) tabla.Rows.Add(l.DetalleFacturaLoteID,l.CantidadDevuelta);
        return tabla;
    }
}
