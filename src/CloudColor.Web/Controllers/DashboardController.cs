using CloudColor.Negocio;
using CloudColor.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CloudColor.Web.Controllers;
[Authorize]
public sealed class DashboardController(Aplicacion app):Controller
{
    [HttpGet("/Dashboard")]
    public IActionResult Index()
    {
        var f=app.Fase2.Facturas();
        var metrics=new List<Metrica>();var graficos=new List<Grafico>();
        var actividad=f.Take(8).Select(x=>new Actividad("Factura",x.NumeroFactura,x.Cliente,x.Estado,x.Fecha,"/Facturas/Ver/"+x.FacturaID)).ToList();
        if(!User.IsInRole("Cajero"))
        {
            var c=app.Fase2.Cotizaciones();var o=app.Fase2.Ordenes();
            var variantes=app.Fase2.Variantes();var inventario=app.Fase2.Inventario();
            var stock=inventario.GroupBy(x=>x.ProductoVarianteID).ToDictionary(x=>x.Key,x=>x.Sum(l=>l.CantidadDisponible));
            int bajo=variantes.Count(v=>stock.GetValueOrDefault(v.ProductoVarianteID)>0&&stock.GetValueOrDefault(v.ProductoVarianteID)<v.StockMinimo);
            metrics.Add(new("Stock bajo",bajo.ToString(),"Ver variantes que requieren atención","/Inventario?estado=Stock%20bajo"));
            metrics.Add(new("Sin stock",variantes.Count(v=>stock.GetValueOrDefault(v.ProductoVarianteID)<=0).ToString(),"Incluye variantes sin ingresos","/Inventario?estado=Sin%20stock"));
            graficos.Add(new("Estado de órdenes","Conteo de órdenes registradas, sin estimaciones.",new[]{"PENDIENTE","PARCIAL","COMPLETA"}.Select(e=>new Barra(e,o.Count(x=>x.Estado==e),"/Compras?estado="+e)).ToList()));
            foreach(var orden in o.OrderByDescending(x=>x.Fecha).Take(8))
                actividad.AddRange(app.Fase2.Recepciones(orden.OrdenCompraID).GroupBy(r=>r.RecepcionCompraID).Select(g=>g.First()).Select(r=>new Actividad("Recepción",r.NumeroRecepcion,orden.Proveedor,"REGISTRADA",r.FechaRecepcion,"/Recepciones/Detalle/"+r.RecepcionCompraID+"?orden="+orden.OrdenCompraID)));
            metrics.AddRange([new("Productos activos",app.Productos.Listar().Count(x=>x.Activo).ToString(),"Tu catálogo principal","/Productos"),new("Variantes",variantes.Count.ToString(),"Presentaciones y colores","/Variantes"),new("Clientes",app.Clientes.Listar().Count.ToString(),"Relaciones que crecen","/Clientes"),new("Proveedores",app.Proveedores.Listar().Count.ToString(),"Tu red de abastecimiento","/Proveedores"),new("Existencias disponibles",inventario.Sum(x=>x.CantidadDisponible).ToString("N2"),"Suma de unidades por variante","/Inventario"),new("Cotizaciones pendientes",c.Count(x=>x.Estado=="PENDIENTE").ToString(),"Por confirmar","/Cotizaciones?estado=PENDIENTE"),new("Órdenes en curso",o.Count(x=>x.Estado is "PENDIENTE" or "PARCIAL").ToString(),"Pendientes y parciales","/Compras")]);
            actividad.AddRange(c.Take(8).Select(x=>new Actividad("Cotización",x.NumeroCotizacion,x.Cliente,x.Estado,x.Fecha,"/Cotizaciones/Ver/"+x.CotizacionID)));
            actividad.AddRange(o.Take(8).Select(x=>new Actividad("Compra",x.NumeroOrden,x.Proveedor,x.Estado,x.Fecha,"/Compras/Ver/"+x.OrdenCompraID)));
        }
        metrics.Add(new("Facturas por cobrar",f.Count(x=>x.Estado=="PENDIENTE_PAGO").ToString(),"Pendientes de pago","/Facturas?estado=PENDIENTE_PAGO"));
        graficos.Add(new("Estado de facturas","Facturas registradas por estado actual.",f.GroupBy(x=>x.Estado).Select(g=>new Barra(g.Key,g.Count(),"/Facturas?estado="+g.Key)).ToList()));
        return View(new DashboardVm(metrics,actividad.OrderByDescending(x=>x.Fecha).Take(8).ToList(),graficos));
    }
}
