using CloudColor.Entidades;
using Microsoft.Data.SqlClient;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class ProveedorDatos(AccesoDatos db)
{
    public List<Proveedor> Listar(int actor, string? buscar = null) => db.Consultar("dbo.sp_ProveedorListar", r => new Proveedor
    {
        ProveedorID = (int)r["ProveedorID"], Activo = (bool)r["Activo"],
        Nombre = (string)r["Nombre"],
        NIT = r["NIT"] == DBNull.Value ? null : (string)r["NIT"],
        Telefono = r["Telefono"] == DBNull.Value ? null : (string)r["Telefono"],
        Correo = r["Correo"] == DBNull.Value ? null : (string)r["Correo"],
        Direccion = r["Direccion"] == DBNull.Value ? null : (string)r["Direccion"]
    }, Entero("@UsuarioActorID", actor), Texto("@Buscar", 180, buscar));
    public int Guardar(int actor, Proveedor e)
    {
        var parametros = new List<SqlParameter>
        {
            Entero("@UsuarioActorID", actor),
            Texto("@Nombre", 180, e.Nombre),
            Texto("@NIT", 30, e.NIT),
            Texto("@Telefono", 30, e.Telefono),
            Texto("@Correo", 150, e.Correo),
            Texto("@Direccion", 250, e.Direccion)
        };
        if (e.ProveedorID == 0) return db.Escalar("dbo.sp_ProveedorInsertar", parametros.ToArray());
        parametros.Add(Entero("@ProveedorID", e.ProveedorID));
        db.Ejecutar("dbo.sp_ProveedorModificar", parametros.ToArray());
        return e.ProveedorID;
    }
    public void CambiarActivo(int actor, int id, bool activo) => db.Ejecutar("dbo.sp_ProveedorCambiarActivo",
        Entero("@UsuarioActorID", actor), Entero("@ProveedorID", id), Bit("@Activo", activo));
}
