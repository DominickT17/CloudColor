using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;
public sealed class EmpleadoServicio(EmpleadoDatos datos, SeguridadServicio seguridad)
{
    public List<Empleado> Listar(string? buscar = null) => datos.Listar(seguridad.ExigirMantenimiento().UsuarioID, Validacion.Texto(buscar, "Busqueda", 180, false));
    public int Guardar(Empleado e)
    {
        int actor = seguridad.ExigirMantenimiento().UsuarioID;
        e.CodigoEmpleado = Validacion.Texto(e.CodigoEmpleado, "CodigoEmpleado", 30, true)!;
        e.Nombres = Validacion.Texto(e.Nombres, "Nombres", 100, true)!;
        e.Apellidos = Validacion.Texto(e.Apellidos, "Apellidos", 100, true)!;
        e.Telefono = Validacion.Texto(e.Telefono, "Telefono", 30, false);
        e.Correo = Validacion.Texto(e.Correo, "Correo", 150, false);
        return datos.Guardar(actor, e);
    }
    public void CambiarActivo(int id, bool activo) => datos.CambiarActivo(seguridad.ExigirMantenimiento().UsuarioID, id, activo);
}
