using System.Globalization;
using CloudColor.Negocio;
using CloudColor.Web.Models;
using CloudColor.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CloudColor.Web.Controllers;
[Authorize(Roles="Digitador,Gerente")]
public sealed class CatalogosController(Aplicacion app) : Controller
{
    private bool Existe(string modulo)=>CatalogosWeb.Rutas.Contains(modulo);
    private bool Permitido(string modulo)=>modulo is not ("Usuarios" or "Roles") || User.IsInRole("Gerente");
    [HttpGet("/{modulo}")]
    public IActionResult Index(string modulo,string? buscar)
    {
        if(!Existe(modulo)) return NotFound(); if(!Permitido(modulo)) return Forbid();
        var c=CatalogosWeb.Obtener(app,modulo); buscar=(buscar??"").Trim();
        if(buscar.Length>180) return BadRequest();
        return View(new ListadoCatalogo(c,c.Listar(buscar),buscar));
    }
    [HttpGet("/{modulo}/Editar/{id:int?}")]
    public IActionResult Editar(string modulo,int id=0,string? buscar=null)
    {
        if(!Existe(modulo)) return NotFound(); if(!Permitido(modulo)) return Forbid();
        var c=CatalogosWeb.Obtener(app,modulo); if(id==0&&!c.Crear)return NotFound();
        var o=id==0 ? Activator.CreateInstance(c.Tipo)! : c.Listar(null).SingleOrDefault(x=>c.ID(x)==id);
        if(o==null)return NotFound();
        return View(new EditorCatalogo(c,id,c.Campos.ToDictionary(f=>f.Nombre,f=>c.Valor(o,f.Nombre)),buscar));
    }
    [HttpPost("/{modulo}/Editar/{id:int?}")]
    public IActionResult Guardar(string modulo,IFormCollection form,int id=0,string? buscar=null)
    {
        if(!Existe(modulo))return NotFound(); if(!Permitido(modulo))return Forbid();
        var c=CatalogosWeb.Obtener(app,modulo); if(id==0&&!c.Crear)return NotFound();
        var o=id==0 ? Activator.CreateInstance(c.Tipo)! : c.Listar(null).SingleOrDefault(x=>c.ID(x)==id);
        if(o==null)return NotFound();
        var valores=new Dictionary<string,string>();
        foreach(var f in c.Campos)
        {
            if(f.Tipo=="readonly") { valores[f.Nombre]=c.Valor(o,f.Nombre); continue; }
            string v=form[f.Nombre].FirstOrDefault()??""; valores[f.Nombre]=v;
            var propiedad=c.Tipo.GetProperty(f.Nombre)!;
            try
            {
                if(f.Requerido&&string.IsNullOrWhiteSpace(v))throw new FormatException();
                if(f.Opciones!=null && v!="" && !f.Opciones.Any(x=>x.Valor==v))throw new FormatException();
                object? dato=propiedad.PropertyType==typeof(bool) ? v is "true" or "True" or "on"
                    : propiedad.PropertyType==typeof(int) ? int.Parse(v,CultureInfo.InvariantCulture)
                    : propiedad.PropertyType==typeof(int?) ? (v==""?null:int.Parse(v,CultureInfo.InvariantCulture))
                    : propiedad.PropertyType==typeof(decimal) ? decimal.Parse(v,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture) : v;
                if(dato is string texto&&texto.Length>f.Maximo)throw new FormatException();
                propiedad.SetValue(o,dato);
            }
            catch(Exception ex) when(ex is FormatException or OverflowException) { ModelState.AddModelError(f.Nombre,"Revisa este campo y selecciona un valor válido."); }
        }
        if(ModelState.IsValid)
        {
            try { c.Guardar(o,form["Password"].FirstOrDefault()); TempData["Exito"]="Registro guardado correctamente."; return RedirectToAction(nameof(Index),new{modulo,buscar}); }
            catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        }
        return View("Editar",new EditorCatalogo(c,id,valores,buscar));
    }
    [HttpPost("/{modulo}/Estado/{id:int}")]
    public IActionResult Estado(string modulo,int id,bool activo,string? buscar)
    {
        if(!Existe(modulo))return NotFound(); if(!Permitido(modulo))return Forbid();
        var c=CatalogosWeb.Obtener(app,modulo); if(c.Activar==null)return NotFound();
        try { c.Activar(id,activo); TempData["Exito"]="Estado actualizado."; }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Index),new{modulo,buscar});
    }
    [HttpPost("/Productos/Marca")]
    public IActionResult Marca(string nombre)
    {
        try { app.Catalogos.CrearMarca(nombre); TempData["Exito"]="Marca creada. Ya puedes seleccionarla al editar un producto."; }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){TempData["Error"]=ex.Message;}
        return Redirect("/Productos");
    }
    [HttpPost("/Variantes/Color")]
    public IActionResult Color(string nombre,string? codigoHex)
    {
        try { app.Fase2.CrearColor(nombre,codigoHex); TempData["Exito"]="Color creado. Ya puedes seleccionarlo en una variante."; }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){TempData["Error"]=ex.Message;}
        return Redirect("/Variantes");
    }
}
