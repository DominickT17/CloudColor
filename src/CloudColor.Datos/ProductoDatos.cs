using CloudColor.Entidades;
using Microsoft.Data.SqlClient;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class ProductoDatos(AccesoDatos db)
{
    public List<Producto> Listar(int actor, string? buscar = null) => db.Consultar("dbo.sp_ProductoListar", r => new Producto
    {
        ProductoID = (int)r["ProductoID"], Activo = (bool)r["Activo"],
        CategoriaProductoID = (int)r["CategoriaProductoID"],
        MarcaID = (int)r["MarcaID"],
        Nombre = (string)r["Nombre"],
        Descripcion = r["Descripcion"] == DBNull.Value ? null : (string)r["Descripcion"],
        ManejaLote = (bool)r["ManejaLote"],
        AplicaPEPS = (bool)r["AplicaPEPS"]
    }, Entero("@UsuarioActorID", actor), Texto("@Buscar", 180, buscar));
    public int Guardar(int actor, Producto e)
    {
        var parametros = new List<SqlParameter>
        {
            Entero("@UsuarioActorID", actor),
            Entero("@CategoriaProductoID", e.CategoriaProductoID),
            Entero("@MarcaID", e.MarcaID),
            Texto("@Nombre", 150, e.Nombre),
            Texto("@Descripcion", 300, e.Descripcion),
            Bit("@ManejaLote", e.ManejaLote),
            Bit("@AplicaPEPS", e.AplicaPEPS)
        };
        if (e.ProductoID == 0) return db.Escalar("dbo.sp_ProductoInsertar", parametros.ToArray());
        parametros.Add(Entero("@ProductoID", e.ProductoID));
        db.Ejecutar("dbo.sp_ProductoModificar", parametros.ToArray());
        return e.ProductoID;
    }
    public void CambiarActivo(int actor, int id, bool activo) => db.Ejecutar("dbo.sp_ProductoCambiarActivo",
        Entero("@UsuarioActorID", actor), Entero("@ProductoID", id), Bit("@Activo", activo));
}
