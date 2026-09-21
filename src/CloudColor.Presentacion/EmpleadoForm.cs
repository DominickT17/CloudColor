using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class EmpleadoForm : FormularioBase<Empleado>
{
    private readonly Aplicacion app;
    private readonly TextBox campoCodigoEmpleado=new() { Name="CodigoEmpleado", MaxLength=30 };
    private readonly TextBox campoNombres=new() { Name="Nombres", MaxLength=100 };
    private readonly TextBox campoApellidos=new() { Name="Apellidos", MaxLength=100 };
    private readonly TextBox campoTelefono=new() { Name="Telefono", MaxLength=30 };
    private readonly TextBox campoCorreo=new() { Name="Correo", MaxLength=150 };
    public EmpleadoForm(Aplicacion app) : base("Empleado")
    {
        this.app=app;
        Campo("Código empleado",campoCodigoEmpleado);
        Campo("Nombres",campoNombres);
        Campo("Apellidos",campoApellidos);
        Campo("Teléfono",campoTelefono);
        Campo("Correo",campoCorreo);
    }
    protected override List<Empleado> Listar(string buscar) => app.Empleados.Listar(buscar);
    protected override void Mostrar(Empleado e)
    {
        campoCodigoEmpleado.Text=e.CodigoEmpleado ?? "";
        campoNombres.Text=e.Nombres ?? "";
        campoApellidos.Text=e.Apellidos ?? "";
        campoTelefono.Text=e.Telefono ?? "";
        campoCorreo.Text=e.Correo ?? "";
    }
    protected override void Limpiar()
    {
        campoCodigoEmpleado.Text="";
        campoNombres.Text="";
        campoApellidos.Text="";
        campoTelefono.Text="";
        campoCorreo.Text="";
    }
    protected override void GuardarRegistro()
    {
        app.Empleados.Guardar(new Empleado
        {
            EmpleadoID=Seleccionado?.EmpleadoID ?? 0,
            CodigoEmpleado=campoCodigoEmpleado.Text,
            Nombres=campoNombres.Text,
            Apellidos=campoApellidos.Text,
            Telefono=campoTelefono.Text,
            Correo=campoCorreo.Text
        });
    }
    protected override void CambiarEstado(Empleado e) => app.Empleados.CambiarActivo(e.EmpleadoID,!e.Activo);
}
