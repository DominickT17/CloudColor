using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class ProveedorForm : FormularioBase<Proveedor>
{
    private readonly Aplicacion app;
    private readonly TextBox campoNombre=new() { Name="Nombre", MaxLength=180 };
    private readonly TextBox campoNIT=new() { Name="NIT", MaxLength=30 };
    private readonly TextBox campoTelefono=new() { Name="Telefono", MaxLength=30 };
    private readonly TextBox campoCorreo=new() { Name="Correo", MaxLength=150 };
    private readonly TextBox campoDireccion=new() { Name="Direccion", MaxLength=250 };
    public ProveedorForm(Aplicacion app) : base("Proveedor")
    {
        this.app=app;
        Campo("Nombre",campoNombre);
        Campo("NIT",campoNIT);
        Campo("Teléfono",campoTelefono);
        Campo("Correo",campoCorreo);
        Campo("Dirección",campoDireccion);
    }
    protected override List<Proveedor> Listar(string buscar) => app.Proveedores.Listar(buscar);
    protected override void Mostrar(Proveedor e)
    {
        campoNombre.Text=e.Nombre ?? "";
        campoNIT.Text=e.NIT ?? "";
        campoTelefono.Text=e.Telefono ?? "";
        campoCorreo.Text=e.Correo ?? "";
        campoDireccion.Text=e.Direccion ?? "";
    }
    protected override void Limpiar()
    {
        campoNombre.Text="";
        campoNIT.Text="";
        campoTelefono.Text="";
        campoCorreo.Text="";
        campoDireccion.Text="";
    }
    protected override void GuardarRegistro()
    {
        app.Proveedores.Guardar(new Proveedor
        {
            ProveedorID=Seleccionado?.ProveedorID ?? 0,
            Nombre=campoNombre.Text,
            NIT=campoNIT.Text,
            Telefono=campoTelefono.Text,
            Correo=campoCorreo.Text,
            Direccion=campoDireccion.Text
        });
    }
    protected override void CambiarEstado(Proveedor e) => app.Proveedores.CambiarActivo(e.ProveedorID,!e.Activo);
}
