using CloudColor.Entidades;
using CloudColor.Negocio;
using CloudColor.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CloudColor.Web.Controllers;
[Authorize]
public sealed class OperacionesController(Aplicacion app):Controller
{
    private const string Ingreso="Gerente,Digitador",CobrarRoles="Gerente,Cajero";
    private string Ruta=>Request.Path.Value!.Split('/')[1];
    private static DocumentoFila Fila(Factura f)=>new(f.FacturaID,f.NumeroFactura,f.Cliente,f.Fecha,f.Estado,f.TotalActual);
    private static DocumentoFila Fila(Cotizacion c)=>new(c.CotizacionID,c.NumeroCotizacion,c.Cliente,c.Fecha,c.Estado,c.Total);
    private static DocumentoFila Fila(OrdenCompra o)=>new(o.OrdenCompraID,o.NumeroOrden,o.Proveedor,o.Fecha,o.Estado);
    private static string Numero(string p)=>p+"-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
    [Authorize(Roles=Ingreso),HttpGet("/Cotizaciones"),HttpGet("/Compras"),HttpGet("/Recepciones")]
    public IActionResult Listar(string? buscar,string? estado)
    {
        var ruta=Ruta;var filas=ruta=="Cotizaciones" ? app.Fase2.Cotizaciones(buscar).Select(Fila).ToList() : app.Fase2.Ordenes(buscar).Select(Fila).ToList();
        if(!string.IsNullOrWhiteSpace(estado))filas=filas.Where(x=>x.Estado==estado).ToList();
        return View("Lista",new DocumentosVm(ruta,ruta=="Compras"?"Órdenes de compra":ruta,ruta=="Cotizaciones"?"De una propuesta clara a la próxima venta.":ruta=="Recepciones"?"Selecciona una orden para consultar sus entregas o registrar un ingreso.":"Tu abastecimiento, desde la orden hasta la última entrega.",filas,buscar,ruta!="Recepciones"));
    }
    [HttpGet("/Facturas"),HttpGet("/Cobros")]
    public IActionResult Facturas(string? buscar,string? estado)
    {
        if(Ruta=="Cobros"&&!User.IsInRole("Cajero")&&!User.IsInRole("Gerente"))return Forbid();
        var f=app.Fase2.Facturas(buscar);if(Ruta=="Cobros")f=f.Where(x=>x.Estado=="PENDIENTE_PAGO").ToList();
        if(!string.IsNullOrWhiteSpace(estado))f=f.Where(x=>x.Estado==estado).ToList();
        return View("Lista",new DocumentosVm(Ruta,Ruta,Ruta=="Cobros"?"Facturas pendientes. Distribuye el total y completa el cobro.":"Cada venta, su detalle y su estado en un mismo lugar.",f.Select(Fila).ToList(),buscar,Ruta=="Facturas"&&!User.IsInRole("Cajero")));
    }
    private EditorDocumentoVm Editor(string ruta,DocumentoInput input)
    {
        List<Opcion> personas=ruta=="Compras" ? app.Proveedores.Listar().Where(x=>x.Activo).Select(x=>new Opcion(x.ProveedorID.ToString(),x.Nombre)).ToList() : app.Clientes.Listar().Where(x=>x.Activo).Select(x=>new Opcion(x.ClienteID.ToString(),x.NombreRazonSocial)).ToList();
        var variantes=app.Fase2.Variantes().Where(x=>x.Activo&&x.ProductoActivo&&x.PresentacionActiva&&x.ColorActivo).ToList();
        if(ruta=="Compras") { var ids=app.Fase2.Asociaciones().Where(x=>x.Activo&&x.ProveedorID==input.PersonaId).Select(x=>x.ProductoVarianteID).ToHashSet();variantes=variantes.Where(x=>ids.Contains(x.ProductoVarianteID)).ToList(); }
        if(ruta=="Facturas"&&input.CotizacionId.HasValue)
        {
            var cot=app.Fase2.Cotizaciones().SingleOrDefault(x=>x.CotizacionID==input.CotizacionId)??throw new ArgumentException("Cotización no disponible.");
            input.PersonaId=cot.ClienteID;input.Detalle=app.Fase2.CotizacionDetalle(cot.CotizacionID);
        }
        else foreach(var l in input.Detalle)
        {
            var v=variantes.SingleOrDefault(x=>x.ProductoVarianteID==l.ProductoVarianteID);
            l.CodigoSKU=v?.CodigoSKU??"No disponible";l.Producto=v?.Producto??"Variante no habilitada";l.Presentacion=v?.Presentacion??"";l.Color=v?.Color;
        }
        var disponible=ruta=="Compras"?new Dictionary<int,decimal>():app.Fase2.Inventario().GroupBy(x=>x.ProductoVarianteID).ToDictionary(x=>x.Key,x=>x.Sum(l=>l.CantidadDisponible));
        return new(ruta,ruta=="Compras"?"Nueva orden de compra":ruta=="Facturas"?"Nueva factura":"Nueva cotización",input,personas,variantes,disponible);
    }
    [Authorize(Roles=Ingreso),HttpGet("/Cotizaciones/Nueva"),HttpGet("/Compras/Nueva"),HttpGet("/Facturas/Nueva")]
    public IActionResult Nueva(int? cotizacion)
    {
        try { return View("Editor",Editor(Ruta,new DocumentoInput{Numero=Numero(Ruta=="Compras"?"OC":Ruta=="Facturas"?"F":"C"),CotizacionId=cotizacion})); }
        catch(ArgumentException){return NotFound();}
    }
    [Authorize(Roles=Ingreso),HttpPost("/Cotizaciones/Nueva"),HttpPost("/Compras/Nueva"),HttpPost("/Facturas/Nueva")]
    public IActionResult Crear(DocumentoInput input,string? accion)
    {
        var ruta=Ruta;
        if(input.Detalle.Count>200){ModelState.AddModelError("","Máximo 200 líneas por documento.");input.Detalle=input.Detalle.Take(200).ToList();}
        if(accion=="Proveedor") { input.Detalle.Clear();ModelState.Clear(); }
        var vm=Editor(ruta,input);
        if(ModelState.IsValid)
        try
        {
            if(accion=="Agregar")
            {
                if(input.CotizacionId.HasValue)throw new ArgumentException("El detalle pertenece a la cotización.");
                var v=vm.Variantes.SingleOrDefault(x=>x.ProductoVarianteID==input.NuevaVariante)??throw new ArgumentException("Selecciona una variante disponible.");
                if(input.NuevaCantidad<=0||input.NuevoPrecio<0)throw new ArgumentException("La cantidad debe ser positiva y el precio no negativo.");
                if(input.Detalle.Any(x=>x.ProductoVarianteID==v.ProductoVarianteID))throw new ArgumentException("Esta variante ya está en el detalle. Quita la fila para reemplazarla.");
                input.Detalle.Add(new LineaVenta{ProductoVarianteID=v.ProductoVarianteID,CodigoSKU=v.CodigoSKU,Producto=v.Producto,Presentacion=v.Presentacion,Color=v.Color,Cantidad=input.NuevaCantidad,PrecioUnitario=input.NuevoPrecio});
            }
            else if(accion?.StartsWith("Quitar:")==true)
            {
                if(input.CotizacionId.HasValue)throw new ArgumentException("El detalle pertenece a la cotización.");
                if(int.TryParse(accion[7..],out int i)&&i>=0&&i<input.Detalle.Count)input.Detalle.RemoveAt(i);
            }
            else if(accion=="Guardar")
            {
                int id=ruta=="Compras"?app.Fase2.CrearOrden(input.Numero,input.PersonaId,input.Detalle.Select(x=>new LineaCompra{ProductoVarianteID=x.ProductoVarianteID,CantidadSolicitada=x.Cantidad,CostoUnitarioOrden=x.PrecioUnitario}).ToList(),input.Observaciones)
                    :ruta=="Cotizaciones"?app.Fase2.CrearCotizacion(input.Numero,input.PersonaId,input.Detalle,input.FechaVencimiento,input.Observaciones)
                    :app.Fase2.Facturar(input.Numero,input.PersonaId,input.CotizacionId.HasValue?[]:input.Detalle,input.CotizacionId);
                TempData["Exito"]="Documento registrado correctamente.";return Redirect($"/{ruta}/Ver/{id}");
            }
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or OverflowException){ModelState.AddModelError("",ex is OverflowException?"Importe fuera del rango permitido.":ex.Message);}
        return View("Editor",vm);
    }
    [Authorize(Roles=Ingreso),HttpPost("/Cotizaciones/Confirmar/{id:int}")]
    public IActionResult Confirmar(int id)
    {
        try { app.Fase2.Confirmar(id);TempData["Exito"]="Cotización confirmada. Ya puedes emitir la factura."; }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){TempData["Error"]=ex.Message;}
        return Redirect("/Cotizaciones/Ver/"+id);
    }
    [Authorize(Roles=Ingreso),HttpGet("/Cotizaciones/Ver/{id:int}"),HttpGet("/Compras/Ver/{id:int}"),HttpGet("/Recepciones/Ver/{id:int}")]
    public IActionResult Ver(int id)
    {
        if(Ruta=="Cotizaciones")
        {
            var c=app.Fase2.Cotizaciones().SingleOrDefault(x=>x.CotizacionID==id);if(c==null)return NotFound();
            return View("Detalle",new DetalleDocumentoVm(Ruta,Fila(c),app.Fase2.CotizacionDetalle(id),[],[],[]));
        }
        var o=app.Fase2.Ordenes().SingleOrDefault(x=>x.OrdenCompraID==id);if(o==null)return NotFound();
        return View("Detalle",new DetalleDocumentoVm("Compras",Fila(o),[],app.Fase2.OrdenDetalle(id),[],app.Fase2.Recepciones(id)));
    }
    [HttpGet("/Facturas/Ver/{id:int}"),HttpGet("/Facturas/Imprimir/{id:int}")]
    public IActionResult VerFactura(int id)
    {
        var f=app.Fase2.Facturas().SingleOrDefault(x=>x.FacturaID==id);if(f==null)return NotFound();
        var detalle=app.Fase2.FacturaDetalle(id);var pagos=app.Fase2.FacturaPagos(id);
        var lotes=User.IsInRole("Gerente")?app.Fase2.FacturaLotes(id):[];
        return View(Request.Path.Value!.Contains("Imprimir")?"Imprimir":"Detalle",new DetalleDocumentoVm("Facturas",Fila(f),detalle,[],pagos,[],f.Emisor,f.NIT,f.UsuarioEmisor,lotes,f.TotalOriginal));
    }
    [Authorize(Roles=Ingreso),HttpGet("/Recepciones/Nueva/{id:int}")]
    public IActionResult Recibir(int id)
    {
        var o=app.Fase2.Ordenes().SingleOrDefault(x=>x.OrdenCompraID==id);if(o==null)return NotFound();
        var detalle=app.Fase2.OrdenDetalle(id);
        return View("Recepcion",new RecepcionVm(o,detalle,app.Fase2.Ubicaciones().Where(x=>x.Activo&&x.BodegaActiva).ToList(),new RecepcionInput{Numero=Numero("RC"),Lineas=detalle.Where(x=>x.CantidadPendiente>0).Select(x=>new RecibirLinea{DetalleOrdenCompraID=x.DetalleOrdenCompraID,CantidadRecibida=x.CantidadPendiente,CostoUnitario=x.CostoUnitarioOrden}).ToList()}));
    }
    [Authorize(Roles=Ingreso),HttpGet("/Recepciones/Detalle/{id:int}")]
    public IActionResult DetalleRecepcion(int id,int orden)
    {
        var o=app.Fase2.Ordenes().SingleOrDefault(x=>x.OrdenCompraID==orden);if(o==null)return NotFound();
        var lineas=app.Fase2.Recepciones(orden).Where(x=>x.RecepcionCompraID==id).ToList();if(lineas.Count==0)return NotFound();
        return View(new RecepcionDetalleVm(o,lineas,app.Fase2.OrdenDetalle(orden)));
    }
    [Authorize(Roles=Ingreso),HttpPost("/Recepciones/Nueva/{id:int}")]
    public IActionResult Recibir(int id,RecepcionInput input)
    {
        var o=app.Fase2.Ordenes().SingleOrDefault(x=>x.OrdenCompraID==id);if(o==null)return NotFound();
        if(ModelState.IsValid)try
        {
            app.Fase2.Recibir(input.Numero,id,input.FechaIngreso,input.Lineas.Where(x=>x.Incluir).Select(x=>new LineaRecepcion{DetalleOrdenCompraID=x.DetalleOrdenCompraID,CantidadRecibida=x.CantidadRecibida,CostoUnitario=x.CostoUnitario,UbicacionID=x.UbicacionID,NumeroLoteProveedor=x.NumeroLoteProveedor,FechaVencimiento=x.FechaVencimiento}).ToList(),input.Observaciones);
            TempData["Exito"]="Recepción registrada. Existencias y estado de la orden actualizados.";return Redirect("/Compras/Ver/"+id);
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        return View("Recepcion",new RecepcionVm(o,app.Fase2.OrdenDetalle(id),app.Fase2.Ubicaciones().Where(x=>x.Activo&&x.BodegaActiva).ToList(),input));
    }
    [Authorize(Roles=CobrarRoles),HttpGet("/Cobros/Registrar/{id:int}")]
    public IActionResult Cobro(int id)=>PantallaCobro(id,new());
    private IActionResult PantallaCobro(int id,CobroInput input)
    {
        var f=app.Fase2.Facturas().SingleOrDefault(x=>x.FacturaID==id);if(f==null)return NotFound();
        return View("Cobro",new CobroVm(f,app.Fase2.FacturaPagos(id),app.Fase2.Medios(),input,app.Fase2.FacturaDetalle(id)));
    }
    [Authorize(Roles=CobrarRoles),HttpPost("/Cobros/Registrar/{id:int}")]
    public IActionResult Cobrar(int id,CobroInput input,string? accion)
    {
        if(ModelState.IsValid)try
        {
            if(accion=="Agregar")
            {
                var m=app.Fase2.Medios().SingleOrDefault(x=>x.ID==input.NuevoMedio)??throw new ArgumentException("Selecciona un medio de pago.");
                if(input.NuevoMonto<=0)throw new ArgumentException("Ingresa un monto positivo.");
                if(input.Pagos.Any(x=>x.MedioPagoID==m.ID))throw new ArgumentException("El medio ya está agregado. Quita la fila para reemplazarla.");
                input.Pagos.Add(new Pago{MedioPagoID=m.ID,Medio=m.Nombre,Monto=input.NuevoMonto,Referencia=input.NuevaReferencia});
            }
            else if(accion?.StartsWith("Quitar:")==true&&int.TryParse(accion[7..],out int i)&&i>=0&&i<input.Pagos.Count)input.Pagos.RemoveAt(i);
            else if(accion=="Cobrar") { app.Fase2.Cobrar(id,input.Pagos);TempData["Exito"]="Cobro completo registrado.";return Redirect("/Facturas/Ver/"+id); }
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        return PantallaCobro(id,input);
    }
    [Authorize(Roles=Ingreso),HttpGet("/Inventario")]
    public IActionResult Inventario(string? buscar,int? variante,int? bodega,string? estado,int? ubicacion)
    {
        var variantes=app.Fase2.Variantes();var bodegas=app.Fase2.Bodegas();var ubicaciones=app.Fase2.Ubicaciones();var inventario=app.Fase2.Inventario();
        var totales=inventario.GroupBy(x=>x.ProductoVarianteID).ToDictionary(x=>x.Key,x=>x.Sum(l=>l.CantidadDisponible));
        var filas=inventario.Select(x=>new InventarioFila(x,totales[x.ProductoVarianteID],variantes.Single(v=>v.ProductoVarianteID==x.ProductoVarianteID).StockMinimo)).Where(x=>(string.IsNullOrWhiteSpace(buscar)||x.Lote.Producto.Contains(buscar,StringComparison.OrdinalIgnoreCase)||x.Lote.CodigoSKU.Contains(buscar,StringComparison.OrdinalIgnoreCase))&&(!variante.HasValue||x.Lote.ProductoVarianteID==variante)&&(!bodega.HasValue||ubicaciones.Any(u=>u.UbicacionID==x.Lote.UbicacionID&&u.BodegaID==bodega))&&(string.IsNullOrEmpty(estado)||x.Estado==estado||(estado=="Disponible"&&x.Lote.CantidadDisponible>0))).ToList();
        if(ubicacion.HasValue)filas=filas.Where(x=>x.Lote.UbicacionID==ubicacion).ToList();
        var sinIngresos=variantes.Where(v=>!totales.ContainsKey(v.ProductoVarianteID)&&(!variante.HasValue||v.ProductoVarianteID==variante)&&(string.IsNullOrWhiteSpace(buscar)||v.Producto.Contains(buscar,StringComparison.OrdinalIgnoreCase)||v.CodigoSKU.Contains(buscar,StringComparison.OrdinalIgnoreCase))&&!bodega.HasValue&&!ubicacion.HasValue&&(string.IsNullOrEmpty(estado)||estado=="Sin stock")).ToList();
        return View(new InventarioVm(filas,variantes,bodegas,buscar,variante,bodega,estado,ubicaciones,ubicacion,sinIngresos,variantes.Count(v=>totales.GetValueOrDefault(v.ProductoVarianteID)>0&&totales.GetValueOrDefault(v.ProductoVarianteID)<v.StockMinimo),variantes.Count(v=>totales.GetValueOrDefault(v.ProductoVarianteID)<=0)));
    }
    [Authorize(Roles="Gerente"),HttpGet("/Reportes/Ventas"),HttpGet("/Reportes/Productos")]
    public IActionResult ReporteVentas(DateTime? desde,DateTime? hasta,string? vista,int? limite)
    {
        var fin=hasta?.Date??DateTime.Today;
        var inicio=desde?.Date??fin.AddDays(-29);
        string tipo=vista switch{"mas-vendidos"=>"MAS_VENDIDOS","mayores-ingresos"=>"MAYORES_INGRESOS","menos-vendidos"=>"MENOS_VENDIDOS",_=>"VENTAS"};
        try
        {
            if(tipo!="VENTAS") limite ??= 5;
            var resumen=app.Fase2.ReporteVentas(inicio,fin);
            var filas=tipo=="VENTAS"?[]:app.Fase2.ReporteProductos(inicio,fin,tipo,limite)
                .Select(p=>new ReporteProducto(p.Producto,p.CodigoSKU,p.Presentacion,p.Color,p.CantidadVendida,p.Ingresos)).ToList();
            return View(new ReporteVentasVm(inicio,fin,resumen.TotalFacturado,resumen.TotalEfectivo,resumen.TotalCheque,
                resumen.TotalTarjeta,resumen.CantidadFacturas,tipo,limite,filas,true));
        }
        catch(ArgumentException ex){return BadRequest(ex.Message);}
    }

    [Authorize(Roles="Gerente"),HttpGet("/Reportes/Inventario")]
    public IActionResult ReporteInventario()=>View("ReporteSinStock",new ReporteSinStockVm(app.Fase2.ReporteSinStock()));

    [Authorize(Roles="Gerente"),HttpGet("/Reportes/Factura")]
    public IActionResult ReporteFactura(string? buscar)=>Redirect("/Facturas"+(string.IsNullOrWhiteSpace(buscar)?"":"?buscar="+Uri.EscapeDataString(buscar)));

    [Authorize(Roles="Gerente"),HttpGet("/Facturas/Anular/{id:int}")]
    public IActionResult AnularFactura(int id)
    {
        var factura=app.Fase2.Facturas().SingleOrDefault(x=>x.FacturaID==id);
        if(factura==null)return NotFound();
        if(factura.Estado=="ANULADA")return Redirect("/Facturas/Ver/"+id);
        return View("AnularFactura",factura);
    }

    [Authorize(Roles="Gerente"),HttpPost("/Facturas/Anular/{id:int}")]
    public IActionResult AnularFactura(int id,string motivo)
    {
        try{app.Fase2.AnularFactura(id,motivo);TempData["Exito"]="Factura anulada; se conservaron sus datos historicos y se restauro el inventario.";}
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){TempData["Error"]=ex.Message;}
        return Redirect("/Facturas/Ver/"+id);
    }

    [Authorize(Roles="Gerente"),HttpGet("/Devoluciones")]
    public IActionResult Devoluciones(string? buscar)=>View(new DevolucionesVm(app.Fase2.Devoluciones(buscar),buscar));

    [Authorize(Roles="Gerente"),HttpGet("/Devoluciones/Nueva")]
    public IActionResult NuevaDevolucion(int? factura)=>CargarDevolucion(factura,new DevolucionInput{Numero=Numero("DV"),FacturaID=factura??0});

    private IActionResult CargarDevolucion(int? factura,DevolucionInput input)
    {
        var facturas=app.Fase2.Facturas().Where(x=>x.Estado!="ANULADA").ToList();
        var disponibles=new List<LineaDevolucion>();
        if(factura.HasValue&&factura.Value>0)
        {
            if(!facturas.Any(x=>x.FacturaID==factura.Value))return NotFound();
            try{disponibles=app.Fase2.DevolucionDisponibles(factura.Value);}
            catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return Redirect("/Devoluciones");}
            if(input.Lineas.Count==0)input.Lineas=disponibles.Select(x=>new DevolucionLineaInput{DetalleFacturaLoteID=x.DetalleFacturaLoteID}).ToList();
        }
        return View("NuevaDevolucion",new DevolucionNuevaVm(facturas,disponibles,input));
    }

    [Authorize(Roles="Gerente"),HttpPost("/Devoluciones/Nueva")]
    public IActionResult NuevaDevolucion(DevolucionInput input)
    {
        try
        {
            var ids=input.Lineas.Where(x=>x.Incluir).Select(x=>x.DetalleFacturaLoteID).ToHashSet();
            var disponibles=app.Fase2.DevolucionDisponibles(input.FacturaID).Where(x=>ids.Contains(x.DetalleFacturaLoteID)).ToList();
            foreach(var linea in disponibles)
            {
                var entrada=input.Lineas.Single(x=>x.DetalleFacturaLoteID==linea.DetalleFacturaLoteID);
                linea.CantidadDevuelta=entrada.CantidadDevuelta;
            }
            app.Fase2.CrearDevolucion(input.Numero,input.FacturaID,input.Motivo,disponibles);
            TempData["Exito"]="Devolucion registrada y existencias restituidas a sus lotes de origen.";
            return Redirect("/Devoluciones");
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        return CargarDevolucion(input.FacturaID,input);
    }

    [Authorize(Roles="Gerente"),HttpGet("/Devoluciones/Ver/{id:int}")]
    public IActionResult VerDevolucion(int id)
    {
        var devolucion=app.Fase2.Devoluciones().SingleOrDefault(x=>x.DevolucionID==id);
        if(devolucion==null)return NotFound();
        return View("DetalleDevolucion",new DevolucionDetalleVm(devolucion,app.Fase2.DevolucionDetalle(id)));
    }
}
