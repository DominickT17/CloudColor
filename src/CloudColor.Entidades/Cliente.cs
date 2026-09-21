namespace CloudColor.Entidades;
public sealed class Cliente
{
    public int ClienteID { get; set; }
    public string TipoCliente { get; set; } = "";
    public string NombreRazonSocial { get; set; } = "";
    public string? NIT { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
}
