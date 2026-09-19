using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class RolesForm : FormularioBase<Rol>
{
    private readonly Aplicacion app;
    private readonly TextBox nombre=new() { Name="Nombre",ReadOnly=true };
    private readonly TextBox descripcion=new() { Name="Descripcion",MaxLength=200 };
    private readonly CheckBox activo=new() { Name="Activo",Text="Habilitado" };
    public RolesForm(Aplicacion app) : base("Roles")
    {
        this.app=app; Nuevo.Visible=false; Activar.Visible=false;
        Campo("Rol",nombre); Campo("Descripción",descripcion); Campo("Estado",activo);
    }
    protected override List<Rol> Listar(string buscar) => app.Seguridad.Roles().Where(r=>r.Nombre.Contains(buscar,StringComparison.OrdinalIgnoreCase)).ToList();
    protected override void Mostrar(Rol r) { nombre.Text=r.Nombre; descripcion.Text=r.Descripcion; activo.Checked=r.Activo; }
    protected override void Limpiar() { nombre.Clear(); descripcion.Clear(); activo.Checked=false; }
    protected override void GuardarRegistro()
    {
        if(Seleccionado==null) throw new ArgumentException("Seleccione un rol.");
        app.Seguridad.GuardarRol(new Rol { RolID=Seleccionado.RolID,Nombre=Seleccionado.Nombre,Descripcion=descripcion.Text,Activo=activo.Checked });
    }
    protected override void CambiarEstado(Rol r) { }
}
