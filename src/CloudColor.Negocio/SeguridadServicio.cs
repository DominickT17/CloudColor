using CloudColor.Datos;
using CloudColor.Entidades;
namespace CloudColor.Negocio;
public sealed class SeguridadServicio(SeguridadDatos datos)
{
    private Sesion? sesion;
    public bool InicialPendiente() => datos.InicialPendiente();
    public void Configurar(string codigo, string nombres, string apellidos, string usuario, string password)
    {
        codigo=Validacion.Texto(codigo,"Codigo de empleado",30,true)!;
        nombres=Validacion.Texto(nombres,"Nombres",100,true)!;
        apellidos=Validacion.Texto(apellidos,"Apellidos",100,true)!;
        usuario=Validacion.Texto(usuario,"Usuario",50,true)!;
        datos.Configurar(codigo,nombres,apellidos,usuario,PasswordHasher.Crear(password));
    }
    public Sesion Login(string usuario, string password)
    {
        sesion=null;
        usuario=Validacion.Texto(usuario,"Usuario",50,true)!;
        Credencial? c=datos.Obtener(usuario);
        if(c == null || !c.Activo || !c.EmpleadoActivo || !c.RolActivo || !PasswordHasher.Verificar(password,c.PasswordHash))
            throw new ArgumentException("Usuario o contraseña incorrectos, o cuenta no habilitada.");
        if(c.Rol is not ("Gerente" or "Digitador" or "Cajero")) throw new ArgumentException("Rol no habilitado.");
        return sesion=new Sesion(c.UsuarioID,c.NombreUsuario,c.Rol);
    }
    // Solo para una identidad previamente autenticada por el host (cookie protegida).
    // Nunca debe exponerse como login por ID ni recibir el actor desde un formulario.
    public Sesion RestaurarSesionValidada(int usuarioId)
    {
        sesion=datos.SesionActual(usuarioId);
        return Actual();
    }
    public void CerrarSesion() => sesion=null;
    public Sesion Actual()
    {
        sesion=sesion == null ? null : datos.SesionActual(sesion.UsuarioID);
        return sesion ?? throw new InvalidOperationException("La sesion ya no esta habilitada. Inicie sesion nuevamente.");
    }
    public Sesion ExigirMantenimiento()
    {
        Sesion s=Actual();
        if(s.Rol is not ("Gerente" or "Digitador")) throw new InvalidOperationException("No tiene permiso para mantenimientos.");
        return s;
    }
    public Sesion ExigirGerente()
    {
        Sesion s=Actual();
        if(s.Rol != "Gerente") throw new InvalidOperationException("Solo Gerente administra usuarios y roles.");
        return s;
    }
    public List<Usuario> Usuarios() => datos.Usuarios(ExigirGerente().UsuarioID);
    public List<Rol> Roles() => datos.Roles(ExigirGerente().UsuarioID);
    public void GuardarRol(Rol r)
    {
        int actor=ExigirGerente().UsuarioID;
        r.Descripcion=Validacion.Texto(r.Descripcion,"Descripcion",200,false);
        datos.GuardarRol(actor,r);
    }
    public int GuardarUsuario(Usuario u,string? password)
    {
        int actor=ExigirGerente().UsuarioID;
        u.NombreUsuario=Validacion.Texto(u.NombreUsuario,"Usuario",50,true)!;
        if(u.EmpleadoID <= 0 || u.RolID <= 0) throw new ArgumentException("Seleccione empleado y rol.");
        if(u.UsuarioID == actor) throw new ArgumentException("Use otra cuenta Gerente para modificar su propia cuenta.");
        string? hash=string.IsNullOrEmpty(password) && u.UsuarioID != 0 ? null : PasswordHasher.Crear(password ?? "");
        return datos.GuardarUsuario(actor,u,hash);
    }
    public void ActivarUsuario(int id,bool activo) => datos.ActivarUsuario(ExigirGerente().UsuarioID,id,activo);
}
