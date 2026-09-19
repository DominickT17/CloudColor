using CloudColor.Entidades;
using static CloudColor.Datos.AccesoDatos;
namespace CloudColor.Datos;
public sealed class SeguridadDatos(AccesoDatos db)
{
    public bool InicialPendiente() => db.Escalar("dbo.sp_ConfiguracionInicialPendiente") == 1;
    public void Configurar(string codigo, string nombres, string apellidos, string usuario, string hash) =>
        db.Ejecutar("dbo.sp_ConfiguracionInicial", Texto("@CodigoEmpleado",30,codigo), Texto("@Nombres",100,nombres),
            Texto("@Apellidos",100,apellidos), Texto("@NombreUsuario",50,usuario), Texto("@PasswordHash",255,hash));
    public Credencial? Obtener(string nombre) => db.Consultar("dbo.sp_UsuarioObtenerPorNombreUsuario",
        r => new Credencial((int)r["UsuarioID"], (string)r["NombreUsuario"], (string)r["PasswordHash"],
            (bool)r["Activo"], (bool)r["EmpleadoActivo"], (bool)r["RolActivo"], (string)r["Rol"]),
        Texto("@NombreUsuario",50,nombre)).SingleOrDefault();
    public Sesion? SesionActual(int id) => db.Consultar("dbo.sp_UsuarioSesion",
        r => new Sesion((int)r["UsuarioID"], (string)r["NombreUsuario"], (string)r["Rol"]), Entero("@UsuarioID",id)).SingleOrDefault();
    public List<Usuario> Usuarios(int actor) => db.Consultar("dbo.sp_UsuarioListar", r => new Usuario
    {
        UsuarioID=(int)r["UsuarioID"], EmpleadoID=(int)r["EmpleadoID"], RolID=(int)r["RolID"],
        NombreUsuario=(string)r["NombreUsuario"], Empleado=(string)r["Empleado"], Rol=(string)r["Rol"], Activo=(bool)r["Activo"]
    }, Entero("@UsuarioActorID",actor));
    public List<Rol> Roles(int actor) => db.Consultar("dbo.sp_RolListar", r => new Rol
    {
        RolID=(int)r["RolID"], Nombre=(string)r["Nombre"], Descripcion=r["Descripcion"] as string, Activo=(bool)r["Activo"]
    }, Entero("@UsuarioActorID",actor));
    public void GuardarRol(int actor, Rol r) => db.Ejecutar("dbo.sp_RolModificar", Entero("@UsuarioActorID",actor),
        Entero("@RolID",r.RolID), Texto("@Descripcion",200,r.Descripcion), Bit("@Activo",r.Activo));
    public int GuardarUsuario(int actor, Usuario u, string? hash)
    {
        if(u.UsuarioID == 0) return db.Escalar("dbo.sp_UsuarioCrear", Entero("@UsuarioActorID",actor),
            Entero("@EmpleadoID",u.EmpleadoID), Entero("@RolID",u.RolID), Texto("@NombreUsuario",50,u.NombreUsuario), Texto("@PasswordHash",255,hash));
        db.EnTransaccion(() =>
        {
            db.Ejecutar("dbo.sp_UsuarioModificar", Entero("@UsuarioActorID",actor), Entero("@UsuarioID",u.UsuarioID),
                Entero("@EmpleadoID",u.EmpleadoID), Texto("@NombreUsuario",50,u.NombreUsuario), Texto("@PasswordHash",255,hash));
            db.Ejecutar("dbo.sp_UsuarioCambiarRol", Entero("@UsuarioActorID",actor), Entero("@UsuarioID",u.UsuarioID), Entero("@RolID",u.RolID));
        });
        return u.UsuarioID;
    }
    public void ActivarUsuario(int actor, int id, bool activo) => db.Ejecutar("dbo.sp_UsuarioCambiarActivo",
        Entero("@UsuarioActorID",actor), Entero("@UsuarioID",id), Bit("@Activo",activo));
}
