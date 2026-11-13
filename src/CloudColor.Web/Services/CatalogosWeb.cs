using CloudColor.Entidades;
using CloudColor.Negocio;
using CloudColor.Web.Models;
namespace CloudColor.Web.Services;
// Describe formularios y columnas; las operaciones siguen perteneciendo a Negocio.
public static class CatalogosWeb
{
    public static readonly string[] Rutas=["Clientes","Proveedores","Productos","Variantes","Bodegas","Ubicaciones","ProveedorProducto","Empleados","Usuarios","Roles"];
    public static CatalogoWeb Obtener(Aplicacion a,string ruta)
    {
        if(ruta is "Usuarios" or "Roles") a.Seguridad.ExigirGerente(); else a.Seguridad.ExigirMantenimiento();
        Campo Texto(string n,string e,bool req=false,int max=250,string tipo="text")=>new(n,e,tipo,req,max);
        Campo Selector(string n,string e,IEnumerable<Opcion> opciones,bool req=true)=>new(n,e,"select",req,Opciones:opciones.ToList());
        List<Opcion> Opciones(IEnumerable<Catalogo> c)=>c.Select(x=>new Opcion(x.ID.ToString(),x.Nombre+(x.Activo?"":" · Inactivo"))).ToList();
        CatalogoWeb Crear<T>(string titulo,string singular,string descripcion,List<Campo> campos,Func<string?,List<T>> listar,Action<T,string?> guardar,Action<int,bool>? activar,bool crear=true) where T:class =>new()
        {Ruta=ruta,Titulo=titulo,Singular=singular,Descripcion=descripcion,Tipo=typeof(T),Campos=campos,Listar=q=>listar(q).Cast<object>().ToList(),Guardar=(o,p)=>guardar((T)o,p),Activar=activar,Crear=crear};
        var contacto=new List<Campo>{Texto("NIT","NIT",max:30),Texto("Telefono","Teléfono",max:30),Texto("Correo","Correo electrónico",max:150,tipo:"email"),Texto("Direccion","Dirección",max:250)};
        switch(ruta)
        {
            case "Clientes": return Crear("Clientes","cliente","Conoce a quienes dan vida a tu negocio.",[Texto("NombreRazonSocial","Nombre o razón social",true,180),Selector("TipoCliente","Tipo de cliente",[new("Particular","Particular"),new("Empresa","Empresa")]),..contacto],a.Clientes.Listar,(x,_)=>a.Clientes.Guardar(x),a.Clientes.CambiarActivo);
            case "Proveedores": return Crear("Proveedores","proveedor","Tus aliados para mantener cada producto disponible.",[Texto("Nombre","Nombre",true,180),..contacto],a.Proveedores.Listar,(x,_)=>a.Proveedores.Guardar(x),a.Proveedores.CambiarActivo);
            case "Productos": return Crear("Productos","producto","La base de tu catálogo. Presentaciones y precios viven en Variantes.",[Texto("Nombre","Nombre",true,150),Selector("CategoriaProductoID","Categoría",Opciones(a.Catalogos.Categorias())),Selector("MarcaID","Marca",Opciones(a.Catalogos.Marcas())),Texto("Descripcion","Descripción",max:300,tipo:"textarea"),new("ManejaLote","Maneja lote","checkbox"),new("AplicaPEPS","Aplica PEPS","checkbox")],a.Productos.Listar,(x,_)=>a.Productos.Guardar(x),a.Productos.CambiarActivo);
            case "Variantes": return Crear("Variantes","variante","Cada presentación, color y precio, claramente identificado.",[Texto("CodigoSKU","SKU",true,50),Selector("ProductoID","Producto",a.Productos.Listar().Select(x=>new Opcion(x.ProductoID.ToString(),x.Nombre))),Selector("PresentacionID","Presentación",Opciones(a.Fase2.Presentaciones())),Selector("ColorID","Color",Opciones(a.Fase2.Colores()),false),new("PrecioVentaActual","Precio de venta (Q)","money",true),new("StockMinimo","Stock mínimo","quantity",true)],a.Fase2.Variantes,(x,_)=>a.Fase2.Guardar(x),a.Fase2.ActivarVariante);
            case "Bodegas": return Crear("Bodegas","bodega","Organiza los espacios que mantienen tu negocio en movimiento.",[Texto("Codigo","Código",true,30),Texto("Nombre","Nombre",true,100),Texto("Direccion","Dirección",max:250)],a.Fase2.Bodegas,(x,_)=>a.Fase2.Guardar(x),a.Fase2.ActivarBodega);
            case "Ubicaciones": return Crear("Ubicaciones","ubicación","Un lugar preciso para cada ingreso de mercadería.",[Selector("BodegaID","Bodega",a.Fase2.Bodegas().Select(x=>new Opcion(x.BodegaID.ToString(),x.Nombre))),Texto("Codigo","Código",true,30),Texto("Descripcion","Descripción",max:200)],a.Fase2.Ubicaciones,(x,_)=>a.Fase2.Guardar(x),a.Fase2.ActivarUbicacion);
            case "ProveedorProducto": return Crear("Proveedor / variante","asociación","Define qué variantes puedes comprar a cada proveedor.",[Selector("ProveedorID","Proveedor",a.Proveedores.Listar().Select(x=>new Opcion(x.ProveedorID.ToString(),x.Nombre))),Selector("ProductoVarianteID","Variante",a.Fase2.Variantes().Select(x=>new Opcion(x.ProductoVarianteID.ToString(),x.Etiqueta)))],a.Fase2.Asociaciones,(x,_)=>a.Fase2.Guardar(x),a.Fase2.ActivarAsociacion);
            case "Empleados": return Crear("Empleados","empleado","Las personas detrás de cada operación.",[Texto("CodigoEmpleado","Código",true,30),Texto("Nombres","Nombres",true,100),Texto("Apellidos","Apellidos",true,100),Texto("Telefono","Teléfono",max:30),Texto("Correo","Correo",max:150,tipo:"email")],a.Empleados.Listar,(x,_)=>a.Empleados.Guardar(x),a.Empleados.CambiarActivo);
            case "Usuarios": return Crear("Usuarios","usuario","Cuentas y accesos de tu equipo, con los perfiles habituales.",[Texto("NombreUsuario","Usuario",true,50),Selector("EmpleadoID","Empleado",a.Empleados.Listar().Select(x=>new Opcion(x.EmpleadoID.ToString(),x.Nombres+" "+x.Apellidos))),Selector("RolID","Rol",a.Seguridad.Roles().Select(x=>new Opcion(x.RolID.ToString(),x.Nombre)))],q=>a.Seguridad.Usuarios().Where(x=>string.IsNullOrEmpty(q)||x.NombreUsuario.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList(),(x,p)=>a.Seguridad.GuardarUsuario(x,p),a.Seguridad.ActivarUsuario);
            case "Roles": return Crear("Roles","rol","Tres perfiles definidos. Administra su descripción y estado.",[Texto("Nombre","Perfil",tipo:"readonly"),Texto("Descripcion","Descripción",max:200,tipo:"textarea"),new("Activo","Perfil activo","checkbox")],q=>a.Seguridad.Roles().Where(x=>string.IsNullOrEmpty(q)||x.Nombre.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList(),(x,_)=>a.Seguridad.GuardarRol(x),null,false);
            default: throw new ArgumentException("Catálogo no disponible.");
        }
    }
}
