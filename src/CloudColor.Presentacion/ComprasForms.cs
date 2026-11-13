using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class OrdenCompraForm : DocumentoForm
{
    private readonly Aplicacion app;
    private readonly ComboBox proveedor=UiFase2.Combo("ProveedorID"),variante=UiFase2.Combo("ProductoVarianteID",380);
    private readonly TextBox numero=new() { Name="Numero",MaxLength=30,Width=240 },observaciones=new() { Name="Observaciones",MaxLength=300,Width=300 };
    private readonly NumericUpDown cantidad=UiFase2.Numero("Cantidad",4),costo=UiFase2.Numero("CostoUnitario");
    private readonly List<LineaCompra> lineas=[];
    public OrdenCompraForm(Aplicacion app) : base("OrdenCompra")
    {
        this.app=app; UiFase2.Campo(Cabecera,"Número de orden",numero); UiFase2.Campo(Cabecera,"Proveedor",proveedor); UiFase2.Campo(Cabecera,"Observaciones",observaciones);
        UiFase2.Campo(Captura,"Variante del proveedor",variante); UiFase2.Campo(Captura,"Cantidad",cantidad); UiFase2.Campo(Captura,"Costo acordado Q",costo);
        Boton(Captura,"Agregar","Agregar",()=>
        {
            if(variante.SelectedItem is not ProductoVariante v || cantidad.Value<=0) throw new ArgumentException("Seleccione variante y cantidad positiva.");
            if(lineas.Any(l=>l.ProductoVarianteID==v.ProductoVarianteID)) throw new ArgumentException("La variante ya está agregada; quite la fila para reemplazarla.");
            lineas.Add(new LineaCompra { ProductoVarianteID=v.ProductoVarianteID,CodigoSKU=v.CodigoSKU,Producto=v.Producto,Presentacion=v.Presentacion,Color=v.Color,CantidadSolicitada=cantidad.Value,CostoUnitarioOrden=costo.Value }); Refrescar();
        });
        Boton(Captura,"Quitar","Quitar fila",()=> { if(Detalle.CurrentRow?.DataBoundItem is LineaCompra l) { lineas.Remove(l); Refrescar(); } });
        Boton(Acciones,"Guardar","Guardar orden",()=> { app.Fase2.CrearOrden(numero.Text,UiFase2.ID(proveedor),lineas,observaciones.Text); Limpiar(); Buscar.Clear(); CargarLista(); Pestanas.SelectedTab=Consulta; Estado.Text="Orden guardada; seleccione para recibir."; });
        Boton(Acciones,"Nuevo","Nuevo",Limpiar);
        Boton(AccionesConsulta,"Recibir","Recibir compra",()=>
        {
            if(Lista.CurrentRow?.DataBoundItem is not OrdenCompra o) throw new ArgumentException("Seleccione una orden.");
            using var f=new RecepcionCompraForm(app,o); f.ShowDialog(this); CargarLista();
        });
        proveedor.SelectedIndexChanged+=(_,_)=>Intentar(()=>
        {
            lineas.Clear(); Refrescar();
            var ids=app.Fase2.Asociaciones().Where(p=>p.Activo && p.ProveedorID==UiFase2.ID(proveedor)).Select(p=>p.ProductoVarianteID).ToHashSet();
            UiFase2.Opciones(variante,UiFase2.Activas(app.Fase2.Variantes()).Where(v=>ids.Contains(v.ProductoVarianteID)).ToList(),"Etiqueta","ProductoVarianteID");
        });
    }
    protected override void Preparar() { UiFase2.Opciones(proveedor,app.Proveedores.Listar().Where(p=>p.Activo).ToList(),"Nombre","ProveedorID"); Limpiar(); }
    private void Limpiar() { numero.Text=UiFase2.NuevoNumero("OC"); observaciones.Clear(); lineas.Clear(); cantidad.Value=1; costo.Value=0; Refrescar(); }
    private void Refrescar() => UiFase2.Mostrar(Detalle,lineas.ToList());
    protected override void CargarLista() { UiFase2.Mostrar(Lista,app.Fase2.Ordenes(Buscar.Text)); SeleccionarConsulta(); }
    protected override void SeleccionarConsulta()
    { UiFase2.Mostrar(DetalleConsulta,Lista.CurrentRow?.DataBoundItem is OrdenCompra o ? app.Fase2.OrdenDetalle(o.OrdenCompraID) : new List<LineaCompra>()); }
}

public sealed class RecepcionCompraForm : DocumentoForm
{
    private readonly Aplicacion app;
    private readonly OrdenCompra orden;
    private readonly ComboBox linea=UiFase2.Combo("DetalleOrdenCompraID",270),ubicacion=UiFase2.Combo("UbicacionID",230);
    private readonly TextBox numero=new() { Name="Numero",MaxLength=30,Width=240 },observaciones=new() { Name="Observaciones",MaxLength=300,Width=230 },lote=new() { Name="NumeroLoteProveedor",MaxLength=60,Width=150 };
    private readonly NumericUpDown cantidad=UiFase2.Numero("CantidadRecibida",4),costo=UiFase2.Numero("CostoUnitario");
    private readonly DateTimePicker ingreso=new() { Name="FechaIngreso",Format=DateTimePickerFormat.Short,Width=130 },vence=new() { Name="FechaVencimiento",Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false,Width=130 };
    private readonly List<LineaRecepcion> lineas=[];
    public RecepcionCompraForm(Aplicacion app,OrdenCompra orden) : base("RecepcionCompra")
    {
        this.app=app; this.orden=orden; Text+=" · "+orden.NumeroOrden;
        ((TableLayoutPanel)Captura.Parent!).RowStyles[1].Height=180;
        UiFase2.Campo(Cabecera,"Número de recepción",numero); UiFase2.Campo(Cabecera,"Fecha de ingreso",ingreso); UiFase2.Campo(Cabecera,"Observaciones",observaciones);
        UiFase2.Campo(Captura,"Línea pendiente",linea); UiFase2.Campo(Captura,"Recibir",cantidad); UiFase2.Campo(Captura,"Costo real Q",costo); UiFase2.Campo(Captura,"Ubicación",ubicacion);
        UiFase2.Campo(Captura,"Lote proveedor",lote); UiFase2.Campo(Captura,"Vence (opcional)",vence);
        Boton(Captura,"Agregar","Agregar",()=>
        {
            if(linea.SelectedItem is not LineaCompra d || ubicacion.SelectedItem is not Ubicacion u) throw new ArgumentException("Seleccione línea y ubicación.");
            if(cantidad.Value<=0 || cantidad.Value>d.CantidadPendiente) throw new ArgumentException("La cantidad debe estar entre cero y lo pendiente.");
            if(lineas.Any(l=>l.DetalleOrdenCompraID==d.DetalleOrdenCompraID)) throw new ArgumentException("La línea ya está agregada; quite la fila para reemplazarla.");
            lineas.Add(new LineaRecepcion { DetalleOrdenCompraID=d.DetalleOrdenCompraID,CodigoSKU=d.CodigoSKU,CantidadRecibida=cantidad.Value,CostoUnitario=costo.Value,UbicacionID=u.UbicacionID,Ubicacion=u.Etiqueta,NumeroLoteProveedor=lote.Text,FechaVencimiento=vence.Checked ? vence.Value.Date : null }); Refrescar();
        });
        Boton(Captura,"Quitar","Quitar fila",()=> { if(Detalle.CurrentRow?.DataBoundItem is LineaRecepcion l) { lineas.Remove(l); Refrescar(); } });
        Boton(Acciones,"Guardar","Registrar recepción",()=> { app.Fase2.Recibir(numero.Text,orden.OrdenCompraID,ingreso.Value.Date,lineas,observaciones.Text); Preparar(); Buscar.Clear(); CargarLista(); Pestanas.SelectedTab=Consulta; Estado.Text="Recepción registrada; inventario y estado de orden actualizados."; });
        linea.SelectedIndexChanged+=(_,_)=> { if(linea.SelectedItem is LineaCompra d) { cantidad.Value=d.CantidadPendiente; costo.Value=d.CostoUnitarioOrden; } };
    }
    protected override void Preparar()
    {
        numero.Text=UiFase2.NuevoNumero("RC"); lineas.Clear(); lote.Clear(); vence.Checked=false; observaciones.Clear();
        UiFase2.Opciones(linea,app.Fase2.OrdenDetalle(orden.OrdenCompraID).Where(d=>d.CantidadPendiente>0).ToList(),"Etiqueta","DetalleOrdenCompraID");
        UiFase2.Opciones(ubicacion,app.Fase2.Ubicaciones().Where(u=>u.Activo && u.BodegaActiva).ToList(),"Etiqueta","UbicacionID"); Refrescar();
        bool pendiente=linea.Items.Count>0;
        NuevoDocumento.Enabled=pendiente;
        Acciones.Controls.Find("Guardar",true).Single().Enabled=pendiente;
        if(!pendiente) Pestanas.SelectedTab=Consulta;
    }
    private void Refrescar() => UiFase2.Mostrar(Detalle,lineas.ToList());
    protected override void CargarLista() { UiFase2.Mostrar(Lista,app.Fase2.Recepciones(orden.OrdenCompraID).Where(r=>r.NumeroRecepcion.Contains(Buscar.Text,StringComparison.OrdinalIgnoreCase)).ToList()); SeleccionarConsulta(); }
    protected override void SeleccionarConsulta() => UiFase2.Mostrar(DetalleConsulta,app.Fase2.OrdenDetalle(orden.OrdenCompraID));
}
