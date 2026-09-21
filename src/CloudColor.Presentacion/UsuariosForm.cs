using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class UsuariosForm : FormularioBase<Usuario>
{
    private readonly Aplicacion app;
    private readonly ComboBox empleado=new() { Name="EmpleadoID",DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly ComboBox rol=new() { Name="RolID",DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly TextBox nombre=new() { Name="NombreUsuario",MaxLength=50 };
    private readonly TextBox password=new() { Name="Password",MaxLength=128,UseSystemPasswordChar=true };
    public UsuariosForm(Aplicacion app) : base("Usuarios")
    {
        this.app=app; Campo("Empleado",empleado); Campo("Rol",rol); Campo("Usuario",nombre);
        Campo("Contraseña nueva",password); Campo("",new Label { Text="12–128 caracteres. Al editar, vacío conserva la contraseña.",AutoSize=true });
        FormClosed+=(_,_)=>password.Clear();
    }
    protected override void Preparar()
    {
        var empleados=app.Empleados.Listar().Where(e=>e.Activo).Select(e=>new Catalogo(e.EmpleadoID,e.Nombres+" "+e.Apellidos,true)).ToList();
        empleado.DisplayMember="Nombre"; empleado.ValueMember="ID"; empleado.DataSource=empleados;
        rol.DisplayMember="Nombre"; rol.ValueMember="RolID"; rol.DataSource=app.Seguridad.Roles().Where(r=>r.Activo).ToList();
    }
    protected override List<Usuario> Listar(string buscar) => app.Seguridad.Usuarios().Where(u=>u.NombreUsuario.Contains(buscar,StringComparison.OrdinalIgnoreCase)).ToList();
    protected override void Mostrar(Usuario u) { empleado.SelectedValue=u.EmpleadoID; rol.SelectedValue=u.RolID; nombre.Text=u.NombreUsuario; password.Clear(); }
    protected override void Limpiar() { empleado.SelectedIndex=-1; rol.SelectedIndex=-1; nombre.Clear(); password.Clear(); }
    protected override void GuardarRegistro()
    {
        app.Seguridad.GuardarUsuario(new Usuario { UsuarioID=Seleccionado?.UsuarioID ?? 0,
            EmpleadoID=empleado.SelectedValue is int e ? e : 0, RolID=rol.SelectedValue is int r ? r : 0, NombreUsuario=nombre.Text },password.Text);
        password.Clear();
    }
    protected override void CambiarEstado(Usuario u) => app.Seguridad.ActivarUsuario(u.UsuarioID,!u.Activo);
}
