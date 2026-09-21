using CloudColor.Datos;
namespace CloudColor.Negocio;
// Composicion sencilla: los formularios reciben servicios, no conexiones ni comandos.
public sealed class Aplicacion : IDisposable
{
    private readonly AccesoDatos db;
    public SeguridadServicio Seguridad { get; }
    public ClienteServicio Clientes { get; }
    public ProveedorServicio Proveedores { get; }
    public ProductoServicio Productos { get; }
    public EmpleadoServicio Empleados { get; }
    public CatalogoServicio Catalogos { get; }
    public Aplicacion(string? cadena = null)
    {
        db=new AccesoDatos(cadena ?? Conexion.LeerCadena());
        Seguridad=new SeguridadServicio(new SeguridadDatos(db));
        Clientes=new ClienteServicio(new ClienteDatos(db),Seguridad);
        Proveedores=new ProveedorServicio(new ProveedorDatos(db),Seguridad);
        Productos=new ProductoServicio(new ProductoDatos(db),Seguridad);
        Empleados=new EmpleadoServicio(new EmpleadoDatos(db),Seguridad);
        Catalogos=new CatalogoServicio(new CatalogoDatos(db),Seguridad);
    }
    public void Dispose() => db.Dispose();
}
