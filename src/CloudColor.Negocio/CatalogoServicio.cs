using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;
public sealed class CatalogoServicio(CatalogoDatos datos, SeguridadServicio seguridad)
{
    public List<Catalogo> Marcas() => datos.Marcas(seguridad.ExigirMantenimiento().UsuarioID);
    public List<Catalogo> Categorias() => datos.Categorias(seguridad.ExigirMantenimiento().UsuarioID);
    public int CrearMarca(string nombre) => datos.CrearMarca(seguridad.ExigirMantenimiento().UsuarioID, Validacion.Texto(nombre,"Marca",100,true)!);
}
