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
        var metrics=new List<Metrica>();
        var actividad=f.Take(8).Select(x=>new Actividad("Factura",x.NumeroFactura,x.Cliente,x.Estado,x.Fecha,"/Facturas/Ver/"+x.FacturaID)).ToList();
        if(!User.IsInRole("Cajero"))
        {
            var c=app.Fase2.Cotizaciones();var o=app.Fase2.Ordenes();
            metrics.AddRange([new("Productos activos",app.Productos.Listar().Count(x=>x.Activo).ToString(),"Tu catálogo principal","/Productos"),new("Variantes",app.Fase2.Variantes().Count.ToString(),"Presentaciones y colores","/Variantes"),new("Clientes",app.Clientes.Listar().Count.ToString(),"Relaciones que crecen","/Clientes"),new("Proveedores",app.Proveedores.Listar().Count.ToString(),"Tu red de abastecimiento","/Proveedores"),new("Existencias disponibles",app.Fase2.Inventario().Sum(x=>x.CantidadDisponible).ToString("N2"),"Suma de unidades por variante","/Inventario"),new("Cotizaciones pendientes",c.Count(x=>x.Estado=="PENDIENTE").ToString(),"Por confirmar","/Cotizaciones"),new("Órdenes en curso",o.Count(x=>x.Estado is "PENDIENTE" or "PARCIAL").ToString(),"Pendientes y parciales","/Compras")]);
            actividad.AddRange(c.Take(8).Select(x=>new Actividad("Cotización",x.NumeroCotizacion,x.Cliente,x.Estado,x.Fecha,"/Cotizaciones/Ver/"+x.CotizacionID)));
            actividad.AddRange(o.Take(8).Select(x=>new Actividad("Compra",x.NumeroOrden,x.Proveedor,x.Estado,x.Fecha,"/Compras/Ver/"+x.OrdenCompraID)));
        }
        metrics.Add(new("Facturas por cobrar",f.Count(x=>x.Estado=="PENDIENTE_PAGO").ToString(),"Pendientes de pago","/Facturas"));
        return View(new DashboardVm(metrics,actividad.OrderByDescending(x=>x.Fecha).Take(8).ToList()));
    }
}
