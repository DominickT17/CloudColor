using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class ProductoVarianteForm : FormularioBase<ProductoVariante>
{
    private readonly Aplicacion app;
    private readonly ComboBox producto=UiFase2.Combo("ProductoID"),presentacion=UiFase2.Combo("PresentacionID"),color=UiFase2.Combo("ColorID");
    private readonly TextBox sku=new() { Name="CodigoSKU",MaxLength=50 };
    private readonly NumericUpDown precio=UiFase2.Numero("PrecioVentaActual"),minimo=UiFase2.Numero("StockMinimo",4);
    public ProductoVarianteForm(Aplicacion app) : base("ProductoVariante")
    {
        this.app=app; Tabla.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells; Tabla.DataBindingComplete+=(_,_)=>UiFase2.Ajustar(Tabla);
        Campo("Producto",producto); Campo("Presentación",presentacion); Campo("Color opcional",color); Campo("SKU",sku);
        Campo("Precio de venta",precio); Campo("Stock mínimo",minimo);
        var nuevoColor=new Button { Name="NuevoColor",Text="Nuevo color...",AutoSize=true }; Campo("Catálogo auxiliar",nuevoColor);
        nuevoColor.Click+=(_,_)=>Intentar(()=>
        {
            using var f=new ColorForm(app); if(f.ShowDialog(this)!=DialogResult.OK) return;
            CargarColores(); color.SelectedValue=f.ColorCreadoID;
        });
    }
    private void CargarColores() => UiFase2.Opciones(color,new List<Catalogo> { new(0,"Sin color",true) }.Concat(app.Fase2.Colores().Where(c=>c.Activo)).ToList(),"Nombre","ID");
    protected override void Preparar()
    {
        UiFase2.Opciones(producto,app.Productos.Listar(),"Nombre","ProductoID");
        UiFase2.Opciones(presentacion,app.Fase2.Presentaciones(),"Nombre","ID"); CargarColores();
    }
    protected override List<ProductoVariante> Listar(string buscar) => app.Fase2.Variantes(buscar);
    protected override void Mostrar(ProductoVariante v)
    {
        producto.SelectedValue=v.ProductoID; presentacion.SelectedValue=v.PresentacionID; color.SelectedValue=v.ColorID ?? 0;
        sku.Text=v.CodigoSKU; precio.Value=v.PrecioVentaActual; minimo.Value=v.StockMinimo;
    }
    protected override void Limpiar() { UiFase2.Ajustar(Tabla); producto.SelectedIndex=-1; presentacion.SelectedIndex=-1; color.SelectedValue=0; sku.Clear(); precio.Value=0; minimo.Value=0; }
    protected override void GuardarRegistro() => app.Fase2.Guardar(new ProductoVariante { ProductoVarianteID=Seleccionado?.ProductoVarianteID ?? 0,ProductoID=UiFase2.ID(producto),PresentacionID=UiFase2.ID(presentacion),ColorID=UiFase2.ID(color)==0 ? null : UiFase2.ID(color),CodigoSKU=sku.Text,PrecioVentaActual=precio.Value,StockMinimo=minimo.Value });
    protected override void CambiarEstado(ProductoVariante v) => app.Fase2.ActivarVariante(v.ProductoVarianteID,!v.Activo);
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        foreach(string n in new[] { "Etiqueta","ProductoActivo","PresentacionActiva","ColorActivo","ManejaLote","AplicaPEPS" }) if(Tabla.Columns[n] is { } columna) columna.Visible=false;
    }
}
public sealed class ColorForm : DialogoBase
{
    public int ColorCreadoID { get; private set; }
    public ColorForm(Aplicacion app) : base("Nuevo color")
    {
        var nombre=new TextBox { Name="Nombre",MaxLength=80 }; var hex=new TextBox { Name="CodigoHex",MaxLength=7 };
        Campo("Nombre",nombre); Campo("#RRGGBB opcional",hex);
        Terminar("Guardar",()=> { ColorCreadoID=app.Fase2.CrearColor(nombre.Text,hex.Text); DialogResult=DialogResult.OK; Close(); });
    }
}
