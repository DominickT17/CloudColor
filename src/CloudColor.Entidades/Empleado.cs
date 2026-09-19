namespace CloudColor.Entidades;
public sealed class Empleado
{
    public int EmpleadoID { get; set; }
    public string CodigoEmpleado { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public bool Activo { get; set; } = true;
}
