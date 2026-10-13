using CloudColor.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CloudColor.Web.Controllers;

[Authorize]
public sealed class BusquedaController(Aplicacion app):Controller
{
    public sealed record Resultado(string Texto,string Ruta);
    public sealed record Grupo(string Nombre,List<Resultado> Resultados);
    [HttpGet("/Buscar")]
    public IActionResult Buscar(string? q)
    {
        q=(q??"").Trim();if(q.Length<2||q.Length>80)return Json(Array.Empty<Grupo>());
        var grupos=new List<Grupo>();
        void Agregar(string nombre,IEnumerable<Resultado> resultados){var lista=resultados.Take(5).ToList();if(lista.Count>0)grupos.Add(new(nombre,lista));}
        // Nunca se consulta un servicio de mantenimiento con el perfil Cajero.
        if(User.IsInRole("Gerente")||User.IsInRole("Digitador"))
        {
            Agregar("Clientes",app.Clientes.Listar(q).Select(x=>new Resultado(x.NombreRazonSocial,"/Clientes/Editar/"+x.ClienteID)));
            Agregar("Productos",app.Productos.Listar(q).Select(x=>new Resultado(x.Nombre,"/Productos/Editar/"+x.ProductoID)));
            Agregar("Variantes",app.Fase2.Variantes(q).Select(x=>new Resultado(x.Etiqueta,"/Variantes/Editar/"+x.ProductoVarianteID)));
            Agregar("Proveedores",app.Proveedores.Listar(q).Select(x=>new Resultado(x.Nombre,"/Proveedores/Editar/"+x.ProveedorID)));
            Agregar("Cotizaciones",app.Fase2.Cotizaciones(q).Select(x=>new Resultado(x.NumeroCotizacion+" · "+x.Cliente,"/Cotizaciones/Ver/"+x.CotizacionID)));
            Agregar("Órdenes",app.Fase2.Ordenes(q).Select(x=>new Resultado(x.NumeroOrden+" · "+x.Proveedor,"/Compras/Ver/"+x.OrdenCompraID)));
        }
        Agregar("Facturas",app.Fase2.Facturas(q).Select(x=>new Resultado(x.NumeroFactura+" · "+x.Cliente,"/Facturas/Ver/"+x.FacturaID)));
        return Json(grupos);
    }
}
