using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class BodegaForm : FormularioBase<Bodega>
{
    private readonly Aplicacion app;
    private readonly TextBox codigo=new() { Name="Codigo",MaxLength=30 },nombre=new() { Name="Nombre",MaxLength=100 },direccion=new() { Name="Direccion",MaxLength=250 };
    public BodegaForm(Aplicacion app) : base("Bodega") { this.app=app; Tabla.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells; Tabla.DataBindingComplete+=(_,_)=>UiFase2.Ajustar(Tabla); Campo("Código",codigo); Campo("Nombre",nombre); Campo("Dirección",direccion); }
    protected override List<Bodega> Listar(string buscar) => app.Fase2.Bodegas(buscar);
    protected override void Mostrar(Bodega b) { codigo.Text=b.Codigo; nombre.Text=b.Nombre; direccion.Text=b.Direccion; }
    protected override void Limpiar() { UiFase2.Ajustar(Tabla); codigo.Clear(); nombre.Clear(); direccion.Clear(); }
    protected override void GuardarRegistro() => app.Fase2.Guardar(new Bodega { BodegaID=Seleccionado?.BodegaID ?? 0,Codigo=codigo.Text,Nombre=nombre.Text,Direccion=direccion.Text });
    protected override void CambiarEstado(Bodega b) => app.Fase2.ActivarBodega(b.BodegaID,!b.Activo);
}
public sealed class UbicacionForm : FormularioBase<Ubicacion>
{
    private readonly Aplicacion app;
    private readonly ComboBox bodega=UiFase2.Combo("BodegaID");
    private readonly TextBox codigo=new() { Name="Codigo",MaxLength=30 },descripcion=new() { Name="Descripcion",MaxLength=200 };
    public UbicacionForm(Aplicacion app) : base("Ubicacion") { this.app=app; Tabla.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells; Tabla.DataBindingComplete+=(_,_)=>UiFase2.Ajustar(Tabla); Campo("Bodega",bodega); Campo("Código",codigo); Campo("Descripción",descripcion); }
    protected override void Preparar() => UiFase2.Opciones(bodega,app.Fase2.Bodegas(),"Nombre","BodegaID");
    protected override List<Ubicacion> Listar(string buscar) => app.Fase2.Ubicaciones(buscar);
    protected override void Mostrar(Ubicacion u) { bodega.SelectedValue=u.BodegaID; codigo.Text=u.Codigo; descripcion.Text=u.Descripcion; }
    protected override void Limpiar() { UiFase2.Ajustar(Tabla); bodega.SelectedIndex=-1; codigo.Clear(); descripcion.Clear(); }
    protected override void GuardarRegistro() => app.Fase2.Guardar(new Ubicacion { UbicacionID=Seleccionado?.UbicacionID ?? 0,BodegaID=UiFase2.ID(bodega),Codigo=codigo.Text,Descripcion=descripcion.Text });
    protected override void CambiarEstado(Ubicacion u) => app.Fase2.ActivarUbicacion(u.UbicacionID,!u.Activo);
}
public sealed class ProveedorProductoForm : FormularioBase<ProveedorProducto>
{
    private readonly Aplicacion app;
    private readonly ComboBox proveedor=UiFase2.Combo("ProveedorID"),variante=UiFase2.Combo("ProductoVarianteID");
    public ProveedorProductoForm(Aplicacion app) : base("ProveedorProducto") { this.app=app; Tabla.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells; Tabla.DataBindingComplete+=(_,_)=>UiFase2.Ajustar(Tabla); Campo("Proveedor",proveedor); Campo("Variante",variante); }
    protected override void Preparar() { UiFase2.Opciones(proveedor,app.Proveedores.Listar(),"Nombre","ProveedorID"); UiFase2.Opciones(variante,app.Fase2.Variantes(),"Etiqueta","ProductoVarianteID"); }
    protected override List<ProveedorProducto> Listar(string buscar) => app.Fase2.Asociaciones(buscar);
    protected override void Mostrar(ProveedorProducto p) { proveedor.SelectedValue=p.ProveedorID; variante.SelectedValue=p.ProductoVarianteID; }
    protected override void Limpiar() { UiFase2.Ajustar(Tabla); proveedor.SelectedIndex=-1; variante.SelectedIndex=-1; }
    protected override void GuardarRegistro() => app.Fase2.Guardar(new ProveedorProducto { ProveedorProductoID=Seleccionado?.ProveedorProductoID ?? 0,ProveedorID=UiFase2.ID(proveedor),ProductoVarianteID=UiFase2.ID(variante) });
    protected override void CambiarEstado(ProveedorProducto p) => app.Fase2.ActivarAsociacion(p.ProveedorProductoID,!p.Activo);
}
public sealed class InventarioForm : Form
{
    public InventarioForm(Aplicacion app)
    {
        Text="CloudColor · Inventario por lote"; Size=new Size(1200,650); MinimumSize=new Size(950,500); Font=new Font("Segoe UI",10);
        var tabla=UiFase2.Tabla("Tabla"); var buscar=new TextBox { Name="Buscar",Width=280,PlaceholderText="Producto o SKU" };
        var consultar=new Button { Name="Consultar",Text="Consultar",AutoSize=true }; var estado=new Label { Dock=DockStyle.Bottom,Height=40 };
        var barra=new FlowLayoutPanel { Dock=DockStyle.Top,Height=50 }; barra.Controls.AddRange([buscar,consultar]); Controls.Add(tabla); Controls.Add(barra); Controls.Add(estado);
        void Cargar() { try { UiFase2.Mostrar(tabla,app.Fase2.Inventario(buscar.Text)); if(tabla.Columns["LoteInventarioID"] is { } columna) columna.Visible=true; if(tabla.Columns["CantidadDisponible"] is { } disponible) disponible.DisplayIndex=4; if(tabla.Columns["CostoUnitario"] is { } costo) costo.DisplayIndex=5; estado.Text="Disponible por ingreso; los lotes agotados conservan su historial."; } catch(InvalidOperationException ex) { estado.Text=ex.Message; } }
        consultar.Click+=(_,_)=>Cargar(); Load+=(_,_)=>Cargar();
    }
}
