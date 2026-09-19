using CloudColor.Entidades;
using Microsoft.Data.SqlClient;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class ClienteDatos(AccesoDatos db)
{
    public List<Cliente> Listar(int actor, string? buscar = null) => db.Consultar("dbo.sp_ClienteListar", r => new Cliente
    {
        ClienteID = (int)r["ClienteID"], Activo = (bool)r["Activo"],
        TipoCliente = (string)r["TipoCliente"],
        NombreRazonSocial = (string)r["NombreRazonSocial"],
        NIT = r["NIT"] == DBNull.Value ? null : (string)r["NIT"],
        Telefono = r["Telefono"] == DBNull.Value ? null : (string)r["Telefono"],
        Correo = r["Correo"] == DBNull.Value ? null : (string)r["Correo"],
        Direccion = r["Direccion"] == DBNull.Value ? null : (string)r["Direccion"]
    }, Entero("@UsuarioActorID", actor), Texto("@Buscar", 180, buscar));
    public int Guardar(int actor, Cliente e)
    {
        var parametros = new List<SqlParameter>
        {
            Entero("@UsuarioActorID", actor),
            Texto("@TipoCliente", 20, e.TipoCliente),
            Texto("@NombreRazonSocial", 180, e.NombreRazonSocial),
            Texto("@NIT", 30, e.NIT),
            Texto("@Telefono", 30, e.Telefono),
            Texto("@Correo", 150, e.Correo),
            Texto("@Direccion", 250, e.Direccion)
        };
        if (e.ClienteID == 0) return db.Escalar("dbo.sp_ClienteInsertar", parametros.ToArray());
        parametros.Add(Entero("@ClienteID", e.ClienteID));
        db.Ejecutar("dbo.sp_ClienteModificar", parametros.ToArray());
        return e.ClienteID;
    }
    public void CambiarActivo(int actor, int id, bool activo) => db.Ejecutar("dbo.sp_ClienteCambiarActivo",
        Entero("@UsuarioActorID", actor), Entero("@ClienteID", id), Bit("@Activo", activo));
}
