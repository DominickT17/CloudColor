using CloudColor.Entidades;
namespace CloudColor.Presentacion;

internal static class UiFase2
{
    public static ComboBox Combo(string nombre,int ancho=250) => new() { Name=nombre,DropDownStyle=ComboBoxStyle.DropDownList,Width=ancho };
    public static NumericUpDown Numero(string nombre,int decimales=2) => new() { Name=nombre,DecimalPlaces=decimales,Maximum=1000000000,Width=110,ThousandsSeparator=true };
    public static void Opciones(ComboBox combo,object datos,string texto,string valor)
    { combo.DisplayMember=texto; combo.ValueMember=valor; combo.DataSource=datos; combo.SelectedIndex=-1; }
    public static int ID(ComboBox combo) => combo.SelectedValue is int id ? id : 0;
    public static DataGridView Tabla(string nombre) => new() { Name=nombre,Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
        MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=Color.White,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,
        AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize };
    public static void Mostrar(DataGridView grid,object datos)
    {
        grid.DataSource=datos;
        Ajustar(grid);
    }
    public static void Ajustar(DataGridView grid)
    {
        foreach(DataGridViewColumn c in grid.Columns)
        {
            if(c.Name.EndsWith("ID",StringComparison.Ordinal) || c.Name is "Etiqueta" or "ProductoActivo" or "PresentacionActiva" or "ColorActivo" or "BodegaActiva") c.Visible=false;
            c.HeaderText=c.Name switch
            {
                "CodigoSKU"=>"SKU", "PrecioVentaActual"=>"Precio actual", "StockMinimo"=>"Stock mínimo",
                "PrecioUnitario"=>"Precio unitario", "CostoUnitario"=>"Costo", "CostoUnitarioOrden"=>"Costo acordado",
                "CantidadSolicitada"=>"Solicitada", "CantidadRecibida"=>"Recibida", "CantidadPendiente"=>"Pendiente",
                "CantidadDisponible"=>"Disponible", "CantidadInicial"=>"Ingreso inicial", "NumeroLoteProveedor"=>"Lote proveedor",
                "FechaIngreso"=>"Fecha ingreso", "FechaVencimiento"=>"Vencimiento", "FechaRecepcion"=>"Fecha recepción",
                "NumeroFactura"=>"Factura", "NumeroCotizacion"=>"Cotización", "NumeroOrden"=>"Orden", "NumeroRecepcion"=>"Recepción",
                "TotalOriginal"=>"Total original", "TotalActual"=>"Total actual", "LoteInventarioID"=>"Lote interno", _=>c.Name
            };
            if(c.ValueType==typeof(decimal)) c.DefaultCellStyle.Format=c.Name.Contains("Cantidad") || c.Name=="StockMinimo" ? "N4" : "N2";
            if(c.ValueType==typeof(DateTime) || c.ValueType==typeof(DateTime?)) c.DefaultCellStyle.Format="g";
        }
    }
    public static void Campo(FlowLayoutPanel panel,string etiqueta,Control control)
    {
        var grupo=new FlowLayoutPanel { AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(6) };
        grupo.Controls.Add(new Label { Text=etiqueta,AutoSize=true }); grupo.Controls.Add(control); panel.Controls.Add(grupo);
    }
    public static string NuevoNumero(string prefijo) => prefijo+"-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
    public static List<ProductoVariante> Activas(IEnumerable<ProductoVariante> variantes) => variantes.Where(v=>v.Activo && v.ProductoActivo && v.PresentacionActiva && v.ColorActivo).ToList();
}

public abstract class DocumentoForm : Form
{
    protected readonly TabControl Pestanas=new() { Dock=DockStyle.Fill };
    protected readonly TabPage NuevoDocumento=new("Nuevo");
    protected readonly TabPage Consulta=new("Consultar");
    protected readonly FlowLayoutPanel Cabecera=new() { Dock=DockStyle.Fill,AutoScroll=true };
    protected readonly FlowLayoutPanel Captura=new() { Dock=DockStyle.Fill,AutoScroll=true };
    protected readonly FlowLayoutPanel Acciones=new() { Dock=DockStyle.Fill,AutoScroll=true };
    protected readonly FlowLayoutPanel AccionesConsulta=new() { Dock=DockStyle.Fill,AutoScroll=true };
    protected readonly DataGridView Detalle=UiFase2.Tabla("Detalle");
    protected readonly DataGridView Lista=UiFase2.Tabla("Lista");
    protected readonly DataGridView DetalleConsulta=UiFase2.Tabla("DetalleConsulta");
    protected readonly TextBox Buscar=new() { Name="Buscar",Width=260,PlaceholderText="Numero o nombre" };
    protected readonly Label Estado=new() { Name="Estado",Dock=DockStyle.Bottom,Height=42,Padding=new Padding(8),ForeColor=Color.DarkSlateBlue };
    protected DocumentoForm(string titulo)
    {
        Text="CloudColor · "+titulo; Name=titulo+"Form"; Size=new Size(1160,780); MinimumSize=new Size(1000,700);
        Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterParent;
        Controls.Add(Pestanas); Controls.Add(Estado); Pestanas.TabPages.AddRange([NuevoDocumento,Consulta]);
        var editor=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=4,ColumnCount=1 };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute,105)); editor.RowStyles.Add(new RowStyle(SizeType.Absolute,100));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent,100)); editor.RowStyles.Add(new RowStyle(SizeType.Absolute,62));
        editor.Controls.Add(Cabecera,0,0); editor.Controls.Add(Captura,0,1); editor.Controls.Add(Detalle,0,2); editor.Controls.Add(Acciones,0,3); NuevoDocumento.Controls.Add(editor);
        var consulta=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=4,ColumnCount=1 };
        consulta.RowStyles.Add(new RowStyle(SizeType.Absolute,45)); consulta.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        consulta.RowStyles.Add(new RowStyle(SizeType.Percent,50)); consulta.RowStyles.Add(new RowStyle(SizeType.Absolute,62));
        var filtro=new FlowLayoutPanel { Dock=DockStyle.Fill }; filtro.Controls.Add(Buscar);
        Boton(filtro,"Consultar","Consultar",()=>CargarLista());
        consulta.Controls.Add(filtro,0,0); consulta.Controls.Add(Lista,0,1); consulta.Controls.Add(DetalleConsulta,0,2); consulta.Controls.Add(AccionesConsulta,0,3); Consulta.Controls.Add(consulta);
        Lista.CurrentCellChanged+=(_,_)=>Intentar(SeleccionarConsulta);
    }
    protected Button Boton(FlowLayoutPanel panel,string nombre,string texto,Action accion)
    {
        var b=new Button { Name=nombre,Text=texto,AutoSize=true,Height=34,Margin=new Padding(6) };
        b.Click+=(_,_)=> { b.Enabled=false; try { Intentar(accion); } finally { b.Enabled=true; } }; panel.Controls.Add(b); return b;
    }
    protected void Intentar(Action accion)
    {
        try { UseWaitCursor=true; Estado.Text=""; accion(); }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or OverflowException)
        { Estado.Text=ex is OverflowException ? "Importe fuera del rango permitido." : ex.Message; }
        finally { UseWaitCursor=false; }
    }
    protected override void OnLoad(EventArgs e) { base.OnLoad(e); Intentar(()=> { Preparar(); CargarLista(); }); }
    protected abstract void Preparar();
    protected abstract void CargarLista();
    protected abstract void SeleccionarConsulta();
}
