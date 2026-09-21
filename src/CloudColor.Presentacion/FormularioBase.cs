namespace CloudColor.Presentacion;

public abstract class FormularioBase<T> : Form where T : class
{
    protected readonly DataGridView Tabla = new() { Name="Tabla", Dock=DockStyle.Fill, ReadOnly=true, AutoGenerateColumns=true,
        AllowUserToAddRows=false, AllowUserToDeleteRows=false, MultiSelect=false, SelectionMode=DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor=Color.White, ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    protected readonly TableLayoutPanel Editor = new() { Dock=DockStyle.Fill, ColumnCount=2, AutoScroll=true, Padding=new Padding(12) };
    protected readonly TextBox Buscar = new() { Name="Buscar", Width=240, PlaceholderText="Buscar por nombre" };
    protected readonly Label Estado = new() { Name="Estado", AutoSize=true, ForeColor=Color.DarkSlateBlue, Padding=new Padding(8) };
    protected readonly Button Nuevo = new() { Name="Nuevo", Text="Nuevo", AutoSize=true };
    protected readonly Button Guardar = new() { Name="Guardar", Text="Guardar", AutoSize=true };
    protected readonly Button Activar = new() { Name="Activar", Text="Activar / desactivar", AutoSize=true };
    protected T? Seleccionado;
    private bool cargando;
    protected FormularioBase(string titulo)
    {
        Text="CloudColor · " + titulo; Name=titulo+"Form"; StartPosition=FormStartPosition.CenterParent;
        Size=new Size(1080,680); MinimumSize=new Size(900,600); Font=new Font("Segoe UI",10);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=4, ColumnCount=1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55));
        var barra=new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(8) };
        var consultar=new Button { Name="Consultar", Text="Consultar", AutoSize=true };
        barra.Controls.AddRange([Buscar,consultar]);
        var cuerpo=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2 };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,57)); cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,43));
        cuerpo.Controls.Add(Tabla,0,0); cuerpo.Controls.Add(Editor,1,0);
        Editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140)); Editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var acciones=new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(8) };
        acciones.Controls.AddRange([Nuevo,Guardar,Activar]);
        layout.Controls.Add(barra,0,0); layout.Controls.Add(cuerpo,0,1); layout.Controls.Add(acciones,0,2); layout.Controls.Add(Estado,0,3);
        Controls.Add(layout);
        consultar.Click+=(_,_)=>Intentar(Recargar);
        Nuevo.Click+=(_,_)=>Intentar(LimpiarSeleccion);
        Guardar.Click+=(_,_)=>Intentar(()=> { GuardarRegistro(); Recargar(); Estado.Text="Registro guardado."; });
        Activar.Click+=(_,_)=>Intentar(()=> { if(Seleccionado==null) throw new ArgumentException("Seleccione un registro."); CambiarEstado(Seleccionado); Recargar(); Estado.Text="Estado actualizado."; });
        Tabla.CurrentCellChanged+=(_,_)=> { if(!cargando && Tabla.CurrentRow?.DataBoundItem is T item) Intentar(()=> { Seleccionado=item; Mostrar(item); }); };
    }
    protected void Campo(string titulo, Control control)
    {
        int fila=Editor.RowCount++; Editor.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        Editor.Controls.Add(new Label { Text=titulo, AutoSize=true, Anchor=AnchorStyles.Left },0,fila);
        control.Dock=DockStyle.Fill; control.Margin=new Padding(3,6,3,6); Editor.Controls.Add(control,1,fila);
    }
    protected void Intentar(Action accion)
    {
        try { UseWaitCursor=true; Estado.ForeColor=Color.DarkSlateBlue; accion(); }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException)
        { Estado.ForeColor=Color.Firebrick; Estado.Text=ex.Message; }
        finally { UseWaitCursor=false; }
    }
    protected override void OnLoad(EventArgs e)
    {
        Editor.RowCount++; Editor.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        base.OnLoad(e); Intentar(()=> { Preparar(); Recargar(); });
    }
    protected virtual void Preparar() { }
    protected void Recargar()
    {
        var datos=Listar(Buscar.Text);
        cargando=true;
        try
        {
            Tabla.DataSource=datos;
            foreach(DataGridViewColumn columna in Tabla.Columns)
            {
                if(columna.Name.EndsWith("ID",StringComparison.Ordinal)) columna.Visible=false;
                columna.HeaderText=columna.Name switch
                {
                    "NombreRazonSocial"=>"Nombre / razón social", "TipoCliente"=>"Tipo", "NombreUsuario"=>"Usuario",
                    "CodigoEmpleado"=>"Código", "ManejaLote"=>"Lotes", "AplicaPEPS"=>"PEPS", _=>columna.Name
                };
            }
            Tabla.ClearSelection();
        }
        finally { cargando=false; }
        LimpiarSeleccion(); Estado.Text=$"{datos.Count} registro(s). Seleccione una fila o pulse Nuevo.";
    }
    private void LimpiarSeleccion() { Seleccionado=null; Tabla.CurrentCell=null; Tabla.ClearSelection(); Limpiar(); Estado.Text="Nuevo registro."; }
    protected abstract List<T> Listar(string buscar);
    protected abstract void Mostrar(T item);
    protected abstract void Limpiar();
    protected abstract void GuardarRegistro();
    protected abstract void CambiarEstado(T item);
}
