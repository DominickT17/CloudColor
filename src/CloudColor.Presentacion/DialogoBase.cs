namespace CloudColor.Presentacion;
public abstract class DialogoBase : Form
{
    protected readonly TableLayoutPanel Campos=new() { Dock=DockStyle.Fill, ColumnCount=2, Padding=new Padding(20) };
    protected readonly Label Mensaje=new() { Name="Mensaje", AutoSize=true, ForeColor=Color.Firebrick, MaximumSize=new Size(460,0) };
    protected readonly Button Aceptar=new() { Name="Aceptar", AutoSize=true, Text="Guardar" };
    protected DialogoBase(string titulo)
    {
        Text="CloudColor · "+titulo; StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",10);
        ClientSize=new Size(540,440); MinimumSize=new Size(540,440);
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155)); Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Controls.Add(Campos); AcceptButton=Aceptar;
    }
    protected void Campo(string titulo,Control control)
    {
        int fila=Campos.RowCount++; Campos.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
        Campos.Controls.Add(new Label { Text=titulo, AutoSize=true, Anchor=AnchorStyles.Left },0,fila);
        control.Dock=DockStyle.Fill; control.Margin=new Padding(3,8,3,8); Campos.Controls.Add(control,1,fila);
    }
    protected void Terminar(string accion,Action guardar)
    {
        Aceptar.Text=accion; Campo("",Aceptar);
        int fila=Campos.RowCount++; Campos.Controls.Add(Mensaje,0,fila); Campos.SetColumnSpan(Mensaje,2);
        Aceptar.Click+=(_,_)=>
        {
            try { Aceptar.Enabled=false; UseWaitCursor=true; guardar(); }
            catch(Exception ex) when(ex is ArgumentException or InvalidOperationException) { Mensaje.Text=ex.Message; }
            finally { Aceptar.Enabled=true; UseWaitCursor=false; }
        };
    }
}
