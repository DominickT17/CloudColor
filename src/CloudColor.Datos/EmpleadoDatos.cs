using CloudColor.Entidades;
using Microsoft.Data.SqlClient;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class EmpleadoDatos(AccesoDatos db)
{
    public List<Empleado> Listar(int actor, string? buscar = null) => db.Consultar("dbo.sp_EmpleadoListar", r => new Empleado
    {
        EmpleadoID = (int)r["EmpleadoID"], Activo = (bool)r["Activo"],
        CodigoEmpleado = (string)r["CodigoEmpleado"],
        Nombres = (string)r["Nombres"],
        Apellidos = (string)r["Apellidos"],
        Telefono = r["Telefono"] == DBNull.Value ? null : (string)r["Telefono"],
        Correo = r["Correo"] == DBNull.Value ? null : (string)r["Correo"]
    }, Entero("@UsuarioActorID", actor), Texto("@Buscar", 180, buscar));
    public int Guardar(int actor, Empleado e)
    {
        var parametros = new List<SqlParameter>
        {
            Entero("@UsuarioActorID", actor),
            Texto("@CodigoEmpleado", 30, e.CodigoEmpleado),
            Texto("@Nombres", 100, e.Nombres),
            Texto("@Apellidos", 100, e.Apellidos),
            Texto("@Telefono", 30, e.Telefono),
            Texto("@Correo", 150, e.Correo)
        };
        if (e.EmpleadoID == 0) return db.Escalar("dbo.sp_EmpleadoInsertar", parametros.ToArray());
        parametros.Add(Entero("@EmpleadoID", e.EmpleadoID));
        db.Ejecutar("dbo.sp_EmpleadoModificar", parametros.ToArray());
        return e.EmpleadoID;
    }
    public void CambiarActivo(int actor, int id, bool activo) => db.Ejecutar("dbo.sp_EmpleadoCambiarActivo",
        Entero("@UsuarioActorID", actor), Entero("@EmpleadoID", id), Bit("@Activo", activo));
}
