using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class ProductoForm : FormularioBase<Producto>
{
    private readonly Aplicacion app;
    private readonly ComboBox campoCategoriaProductoID=new() { Name="CategoriaProductoID", DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly ComboBox campoMarcaID=new() { Name="MarcaID", DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly TextBox campoNombre=new() { Name="Nombre", MaxLength=150 };
    private readonly TextBox campoDescripcion=new() { Name="Descripcion", MaxLength=300 };
    private readonly CheckBox campoManejaLote=new() { Name="ManejaLote" };
    private readonly CheckBox campoAplicaPEPS=new() { Name="AplicaPEPS" };
    public ProductoForm(Aplicacion app) : base("Producto")
    {
        this.app=app;
        Campo("Categoría",campoCategoriaProductoID);
        Campo("Marca",campoMarcaID);
        Campo("Nombre",campoNombre);
        Campo("Descripción",campoDescripcion);
        Campo("Maneja lote",campoManejaLote);
        Campo("Aplica PEPS",campoAplicaPEPS);
        var nuevaMarca=new Button { Name="NuevaMarca",Text="Nueva marca...",AutoSize=true };
        Campo("Catálogo auxiliar",nuevaMarca);
        nuevaMarca.Click+=(_,_)=>Intentar(()=>
        {
            using var dialogo=new MarcaForm(app);
            if(dialogo.ShowDialog(this)==DialogResult.OK) Preparar();
        });
    }
    protected override void Preparar()
    {
        campoCategoriaProductoID.DisplayMember="Nombre"; campoCategoriaProductoID.ValueMember="ID";
        campoCategoriaProductoID.DataSource=app.Catalogos.Categorias().Where(x=>x.Activo).ToList();
        campoMarcaID.DisplayMember="Nombre"; campoMarcaID.ValueMember="ID";
        campoMarcaID.DataSource=app.Catalogos.Marcas().Where(x=>x.Activo).ToList();
    }
    protected override List<Producto> Listar(string buscar) => app.Productos.Listar(buscar);
    protected override void Mostrar(Producto e)
    {
        campoCategoriaProductoID.SelectedValue=e.CategoriaProductoID;
        campoMarcaID.SelectedValue=e.MarcaID;
        campoNombre.Text=e.Nombre ?? "";
        campoDescripcion.Text=e.Descripcion ?? "";
        campoManejaLote.Checked=e.ManejaLote;
        campoAplicaPEPS.Checked=e.AplicaPEPS;
    }
    protected override void Limpiar()
    {
        campoCategoriaProductoID.SelectedIndex=-1;
        campoMarcaID.SelectedIndex=-1;
        campoNombre.Text="";
        campoDescripcion.Text="";
        campoManejaLote.Checked=false;
        campoAplicaPEPS.Checked=false;
    }
    protected override void GuardarRegistro()
    {
        app.Productos.Guardar(new Producto
        {
            ProductoID=Seleccionado?.ProductoID ?? 0,
            CategoriaProductoID=campoCategoriaProductoID.SelectedValue is int idCategoriaProductoID ? idCategoriaProductoID : 0,
            MarcaID=campoMarcaID.SelectedValue is int idMarcaID ? idMarcaID : 0,
            Nombre=campoNombre.Text,
            Descripcion=campoDescripcion.Text,
            ManejaLote=campoManejaLote.Checked,
            AplicaPEPS=campoAplicaPEPS.Checked
        });
    }
    protected override void CambiarEstado(Producto e) => app.Productos.CambiarActivo(e.ProductoID,!e.Activo);
}
