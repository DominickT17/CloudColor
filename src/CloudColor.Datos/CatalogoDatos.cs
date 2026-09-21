using CloudColor.Entidades;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class CatalogoDatos(AccesoDatos db)
{
    // Procedimientos elegidos por codigo, nunca por texto escrito por el usuario.
    private List<Catalogo> Listar(string procedimiento, string columna, int actor) => db.Consultar(procedimiento,
        r => new Catalogo((int)r[columna], (string)r["Nombre"], (bool)r["Activo"]), Entero("@UsuarioActorID",actor));
    public List<Catalogo> Marcas(int actor) => Listar("dbo.sp_MarcaListar","MarcaID",actor);
    public List<Catalogo> Categorias(int actor) => Listar("dbo.sp_CategoriaProductoListar","CategoriaProductoID",actor);
    public int CrearMarca(int actor, string nombre) => db.Escalar("dbo.sp_MarcaInsertar", Entero("@UsuarioActorID",actor), Texto("@Nombre",100,nombre));
}
