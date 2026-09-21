using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;
public sealed class ProductoServicio(ProductoDatos datos, SeguridadServicio seguridad)
{
    public List<Producto> Listar(string? buscar = null) => datos.Listar(seguridad.ExigirMantenimiento().UsuarioID, Validacion.Texto(buscar, "Busqueda", 180, false));
    public int Guardar(Producto e)
    {
        int actor = seguridad.ExigirMantenimiento().UsuarioID;
        if (e.CategoriaProductoID <= 0) throw new ArgumentException("Seleccione CategoriaProducto.");
        if (e.MarcaID <= 0) throw new ArgumentException("Seleccione Marca.");
        e.Nombre = Validacion.Texto(e.Nombre, "Nombre", 150, true)!;
        e.Descripcion = Validacion.Texto(e.Descripcion, "Descripcion", 300, false);
        return datos.Guardar(actor, e);
    }
    public void CambiarActivo(int id, bool activo) => datos.CambiarActivo(seguridad.ExigirMantenimiento().UsuarioID, id, activo);
}
