using CloudColor.Entidades;
using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class CotizacionForm(Aplicacion app) : VentaForm(app,false);
public sealed class FacturacionForm(Aplicacion app,Cotizacion? cotizacion=null) : VentaForm(app,true,cotizacion);

public class VentaForm : DocumentoForm
{
    private readonly Aplicacion app;
    private readonly bool factura;
    private Cotizacion? origen;
    private readonly ComboBox cliente=UiFase2.Combo("ClienteID"),variante=UiFase2.Combo("ProductoVarianteID",380);
    private readonly TextBox numero=new() { Name="Numero",MaxLength=30,Width=240 },observaciones=new() { Name="Observaciones",MaxLength=300,Width=240 };
    private readonly DateTimePicker vence=new() { Name="FechaVencimiento",Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false,Width=140 };
    private readonly NumericUpDown cantidad=UiFase2.Numero("Cantidad",4),precio=UiFase2.Numero("PrecioUnitario");
    private readonly Label total=new() { Name="Total",AutoSize=true,Padding=new Padding(12),Font=new Font("Segoe UI",12,FontStyle.Bold) };
    private readonly List<LineaVenta> lineas=[];
    public VentaForm(Aplicacion app,bool factura,Cotizacion? cotizacion=null) : base(factura ? "Facturacion" : "Cotizacion")
    {
        this.app=app; this.factura=factura; origen=cotizacion;
        UiFase2.Campo(Cabecera,"Número",numero); UiFase2.Campo(Cabecera,"Cliente",cliente);
        if(!factura) { UiFase2.Campo(Cabecera,"Vence (opcional)",vence); UiFase2.Campo(Cabecera,"Observaciones",observaciones); }
        UiFase2.Campo(Captura,"Variante",variante); UiFase2.Campo(Captura,"Cantidad",cantidad); UiFase2.Campo(Captura,"Precio unitario",precio);
        Boton(Captura,"Agregar","Agregar",Agregar);
        Boton(Captura,"Quitar","Quitar fila",()=> { if(origen!=null) throw new ArgumentException("El detalle procede de la cotización."); if(Detalle.CurrentRow?.DataBoundItem is LineaVenta l) { lineas.Remove(l); Refrescar(); } });
        Boton(Acciones,"Guardar",factura ? "Emitir factura" : "Guardar cotización",GuardarDocumento);
        Boton(Acciones,"Nuevo","Nuevo",Limpiar); Acciones.Controls.Add(total);
        variante.SelectedIndexChanged+=(_,_)=> { if(variante.SelectedItem is ProductoVariante v) precio.Value=v.PrecioVentaActual; };
        if(!factura)
        {
            Boton(AccionesConsulta,"Confirmar","Confirmar",()=> { var c=ElegirCotizacion(); app.Fase2.Confirmar(c.CotizacionID); CargarLista(); Estado.Text="Cotización confirmada."; });
            Boton(AccionesConsulta,"Facturar","Facturar cotización",()=>
            {
                var c=ElegirCotizacion(); if(c.Estado!="CONFIRMADA") throw new ArgumentException("Confirme primero la cotización.");
                using var f=new FacturacionForm(app,c); f.ShowDialog(this); CargarLista();
            });
        }
        else
        {
            bool cobrar=app.Seguridad.Actual().Rol is "Cajero" or "Gerente";
            Boton(AccionesConsulta,"Cobrar","Cobrar",()=> { app.Fase2.ExigirCobro(); using var f=new PagoForm(app,ElegirFactura()); f.ShowDialog(this); CargarLista(); }).Enabled=cobrar;
            Boton(AccionesConsulta,"VistaPrevia","Vista previa / imprimir",()=>
            {
                var f=ElegirFactura(); using var documento=FacturaImpresion.Crear(f,app.Fase2.FacturaDetalle(f.FacturaID),app.Fase2.FacturaPagos(f.FacturaID));
                if(!documento.PrinterSettings.IsValid) throw new InvalidOperationException("Configure una impresora Windows para abrir la vista previa.");
                using var vista=new PrintPreviewDialog { Document=documento,Width=1050,Height=780 }; vista.ShowDialog(this);
            });
        }
    }
    protected override void Preparar()
    {
        if(factura && app.Seguridad.Actual().Rol=="Cajero") { Pestanas.TabPages.Remove(NuevoDocumento); return; }
        UiFase2.Opciones(cliente,app.Clientes.Listar(),"NombreRazonSocial","ClienteID");
        UiFase2.Opciones(variante,UiFase2.Activas(app.Fase2.Variantes()),"Etiqueta","ProductoVarianteID");
        numero.Text=UiFase2.NuevoNumero(factura ? "F" : "C"); cantidad.Value=1;
        if(origen!=null)
        {
            cliente.SelectedValue=origen.ClienteID; cliente.Enabled=false; Captura.Enabled=false;
            lineas.AddRange(app.Fase2.CotizacionDetalle(origen.CotizacionID));
            Estado.Text="Desde cotización "+origen.NumeroCotizacion+"; precios históricos conservados.";
        }
        Refrescar();
    }
    private void Limpiar()
    {
        origen=null; cliente.Enabled=true; Captura.Enabled=true; cliente.SelectedIndex=-1; variante.SelectedIndex=-1;
        numero.Text=UiFase2.NuevoNumero(factura ? "F" : "C"); observaciones.Clear(); vence.Checked=false; cantidad.Value=1; precio.Value=0; lineas.Clear(); Refrescar();
    }
    private void Agregar()
    {
        if(origen!=null) throw new ArgumentException("El detalle procede de la cotización.");
        if(variante.SelectedItem is not ProductoVariante v) throw new ArgumentException("Seleccione una variante.");
        if(cantidad.Value<=0) throw new ArgumentException("La cantidad debe ser positiva.");
        if(lineas.Any(l=>l.ProductoVarianteID==v.ProductoVarianteID)) throw new ArgumentException("La variante ya está agregada. Quite la fila para reemplazarla.");
        lineas.Add(new LineaVenta { ProductoVarianteID=v.ProductoVarianteID,CodigoSKU=v.CodigoSKU,Producto=v.Producto,Presentacion=v.Presentacion,Color=v.Color,Cantidad=cantidad.Value,PrecioUnitario=precio.Value }); Refrescar();
    }
    private void Refrescar() { UiFase2.Mostrar(Detalle,lineas.ToList()); total.Text=$"Total Q {Fase2Servicio.Total(lineas):N2}"; }
    private void GuardarDocumento()
    {
        int id=factura ? app.Fase2.Facturar(numero.Text,UiFase2.ID(cliente),origen==null ? lineas : [],origen?.CotizacionID)
            : app.Fase2.CrearCotizacion(numero.Text,UiFase2.ID(cliente),lineas,vence.Checked ? vence.Value.Date : null,observaciones.Text);
        Limpiar(); Buscar.Clear(); CargarLista(); Pestanas.SelectedTab=Consulta;
        foreach(DataGridViewRow fila in Lista.Rows)
        {
            bool encontrada=fila.DataBoundItem is Factura f && f.FacturaID==id || fila.DataBoundItem is Cotizacion c && c.CotizacionID==id;
            if(encontrada) { Lista.CurrentCell=fila.Cells.Cast<DataGridViewCell>().First(c=>c.Visible); fila.Selected=true; break; }
        }
        Estado.Text=factura ? "Factura emitida; inventario actualizado. Cobrar desde Consultar." : "Cotización guardada; puede confirmarla desde Consultar.";
    }
    protected override void CargarLista()
    {
        if(factura) UiFase2.Mostrar(Lista,app.Fase2.Facturas(Buscar.Text)); else UiFase2.Mostrar(Lista,app.Fase2.Cotizaciones(Buscar.Text));
        SeleccionarConsulta();
    }
    protected override void SeleccionarConsulta()
    {
        if(Lista.CurrentRow?.DataBoundItem is Factura f) UiFase2.Mostrar(DetalleConsulta,app.Fase2.FacturaDetalle(f.FacturaID));
        else if(Lista.CurrentRow?.DataBoundItem is Cotizacion c) UiFase2.Mostrar(DetalleConsulta,app.Fase2.CotizacionDetalle(c.CotizacionID));
        else UiFase2.Mostrar(DetalleConsulta,new List<LineaVenta>());
    }
    private Cotizacion ElegirCotizacion() => Lista.CurrentRow?.DataBoundItem as Cotizacion ?? throw new ArgumentException("Seleccione una cotización.");
    private Factura ElegirFactura() => Lista.CurrentRow?.DataBoundItem as Factura ?? throw new ArgumentException("Seleccione una factura.");
}

public sealed class PagoForm : Form
{
    private readonly List<Pago> pagos=[];
    public PagoForm(Aplicacion app,Factura factura)
    {
        Text="CloudColor · Cobro "+factura.NumeroFactura; Size=new Size(900,550); MinimumSize=new Size(800,500); Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterParent;
        var medio=UiFase2.Combo("MedioPagoID",190); var monto=UiFase2.Numero("Monto"); var referencia=new TextBox { Name="Referencia",MaxLength=100,Width=220 };
        var tabla=UiFase2.Tabla("Pagos"); var estado=new Label { Name="Estado",Dock=DockStyle.Bottom,Height=50 };
        var panel=new FlowLayoutPanel { Dock=DockStyle.Top,Height=130 }; var acciones=new FlowLayoutPanel { Dock=DockStyle.Bottom,Height=55 };
        var agregar=new Button { Name="Agregar",Text="Agregar",AutoSize=true }; var quitar=new Button { Name="Quitar",Text="Quitar",AutoSize=true };
        var cobrar=new Button { Name="Cobrar",Text="Registrar cobro completo",AutoSize=true }; var total=new Label { AutoSize=true,Padding=new Padding(10) };
        UiFase2.Campo(panel,"Medio",medio); UiFase2.Campo(panel,"Monto Q",monto); UiFase2.Campo(panel,"Referencia cheque / tarjeta",referencia); panel.Controls.AddRange([agregar,quitar]); acciones.Controls.AddRange([cobrar,total]);
        Controls.Add(tabla); Controls.Add(panel); Controls.Add(acciones); Controls.Add(estado);
        void Intentar(Action a) { try { estado.Text=""; a(); } catch(Exception ex) when(ex is ArgumentException or InvalidOperationException) { estado.Text=ex.Message; } }
        void Refrescar() { UiFase2.Mostrar(tabla,pagos.ToList()); if(tabla.Columns["FechaHora"] is { } fecha) fecha.Visible=false; total.Text=$"Total Q {factura.TotalActual:N2} · Distribuido Q {pagos.Sum(p=>p.Monto):N2}"; }
        Load+=(_,_)=>Intentar(()=> { app.Fase2.ExigirCobro(); UiFase2.Opciones(medio,app.Fase2.Medios(),"Nombre","ID"); Refrescar(); });
        agregar.Click+=(_,_)=>Intentar(()=>
        {
            if(medio.SelectedItem is not Catalogo m || monto.Value<=0) throw new ArgumentException("Seleccione medio e importe positivo.");
            if(pagos.Any(p=>p.MedioPagoID==m.ID)) throw new ArgumentException("Ese medio ya está agregado; quite la fila para reemplazarla.");
            pagos.Add(new Pago { MedioPagoID=m.ID,Medio=m.Nombre,Monto=monto.Value,Referencia=referencia.Text }); Refrescar();
        });
        quitar.Click+=(_,_)=> { if(tabla.CurrentRow?.DataBoundItem is Pago p) { pagos.Remove(p); Refrescar(); } };
        cobrar.Click+=(_,_)=> { cobrar.Enabled=false; try { Intentar(()=> { app.Fase2.Cobrar(factura.FacturaID,pagos); DialogResult=DialogResult.OK; Close(); }); } finally { cobrar.Enabled=true; } };
    }
}
