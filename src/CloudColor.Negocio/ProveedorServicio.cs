using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;
public sealed class ProveedorServicio(ProveedorDatos datos, SeguridadServicio seguridad)
{
    public List<Proveedor> Listar(string? buscar = null) => datos.Listar(seguridad.ExigirMantenimiento().UsuarioID, Validacion.Texto(buscar, "Busqueda", 180, false));
    public int Guardar(Proveedor e)
    {
        int actor = seguridad.ExigirMantenimiento().UsuarioID;
        e.Nombre = Validacion.Texto(e.Nombre, "Nombre", 180, true)!;
        e.NIT = Validacion.Texto(e.NIT, "NIT", 30, false);
        e.Telefono = Validacion.Texto(e.Telefono, "Telefono", 30, false);
        e.Correo = Validacion.Texto(e.Correo, "Correo", 150, false);
        e.Direccion = Validacion.Texto(e.Direccion, "Direccion", 250, false);
        return datos.Guardar(actor, e);
    }
    public void CambiarActivo(int id, bool activo) => datos.CambiarActivo(seguridad.ExigirMantenimiento().UsuarioID, id, activo);
}
