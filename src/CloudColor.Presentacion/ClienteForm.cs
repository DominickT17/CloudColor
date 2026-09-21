using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class ClienteForm : FormularioBase<Cliente>
{
    private readonly Aplicacion app;
    private readonly ComboBox campoTipoCliente=new() { Name="TipoCliente", DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly TextBox campoNombreRazonSocial=new() { Name="NombreRazonSocial", MaxLength=180 };
    private readonly TextBox campoNIT=new() { Name="NIT", MaxLength=30 };
    private readonly TextBox campoTelefono=new() { Name="Telefono", MaxLength=30 };
    private readonly TextBox campoCorreo=new() { Name="Correo", MaxLength=150 };
    private readonly TextBox campoDireccion=new() { Name="Direccion", MaxLength=250 };
    public ClienteForm(Aplicacion app) : base("Cliente")
    {
        this.app=app;
        Campo("Tipo de cliente",campoTipoCliente);
        Campo("Nombre / razón social",campoNombreRazonSocial);
        Campo("NIT",campoNIT);
        Campo("Teléfono",campoTelefono);
        Campo("Correo",campoCorreo);
        Campo("Dirección",campoDireccion);
        campoTipoCliente.Items.AddRange(["Particular","Empresa"]);
    }
    protected override List<Cliente> Listar(string buscar) => app.Clientes.Listar(buscar);
    protected override void Mostrar(Cliente e)
    {
        campoTipoCliente.SelectedItem=e.TipoCliente;
        campoNombreRazonSocial.Text=e.NombreRazonSocial ?? "";
        campoNIT.Text=e.NIT ?? "";
        campoTelefono.Text=e.Telefono ?? "";
        campoCorreo.Text=e.Correo ?? "";
        campoDireccion.Text=e.Direccion ?? "";
    }
    protected override void Limpiar()
    {
        campoTipoCliente.SelectedIndex=-1;
        campoNombreRazonSocial.Text="";
        campoNIT.Text="";
        campoTelefono.Text="";
        campoCorreo.Text="";
        campoDireccion.Text="";
    }
    protected override void GuardarRegistro()
    {
        app.Clientes.Guardar(new Cliente
        {
            ClienteID=Seleccionado?.ClienteID ?? 0,
            TipoCliente=campoTipoCliente.Text,
            NombreRazonSocial=campoNombreRazonSocial.Text,
            NIT=campoNIT.Text,
            Telefono=campoTelefono.Text,
            Correo=campoCorreo.Text,
            Direccion=campoDireccion.Text
        });
    }
    protected override void CambiarEstado(Cliente e) => app.Clientes.CambiarActivo(e.ClienteID,!e.Activo);
}
