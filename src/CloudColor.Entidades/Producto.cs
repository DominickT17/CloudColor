namespace CloudColor.Entidades;
public sealed class Producto
{
    public int ProductoID { get; set; }
    public int CategoriaProductoID { get; set; }
    public int MarcaID { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool ManejaLote { get; set; }
    public bool AplicaPEPS { get; set; }
    public bool Activo { get; set; } = true;
}
