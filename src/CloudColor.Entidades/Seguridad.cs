namespace CloudColor.Entidades;
public sealed record Sesion(int UsuarioID, string NombreUsuario, string Rol);
public sealed record Credencial(int UsuarioID, string NombreUsuario, string PasswordHash, bool Activo, bool EmpleadoActivo, bool RolActivo, string Rol);
public sealed record Catalogo(int ID, string Nombre, bool Activo);
public sealed class Usuario
{
    public int UsuarioID { get; set; }
    public int EmpleadoID { get; set; }
    public int RolID { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string Empleado { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool Activo { get; set; }
}
public sealed class Rol
{
    public int RolID { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
