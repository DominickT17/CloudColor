using System.Transactions;
using CloudColor.Entidades;
using CloudColor.Negocio;
using CloudColor.Presentacion;

internal static partial class Program
{
    private static void Elegir(Form f,string control,int id) => Control<ComboBox>(f,control).SelectedValue=id;
    private static void Valor(Form f,string control,decimal valor) => Control<NumericUpDown>(f,control).Value=valor;
    private static void AbrirDesdeMenu<T>(Form menu,string boton) where T:Form
    {
        bool abierto=false;
        using var timer=new System.Windows.Forms.Timer { Interval=100 };
        timer.Tick+=(_,_)=>
        {
            var f=Application.OpenForms.OfType<T>().SingleOrDefault();
            if(f==null) return;
            timer.Stop(); abierto=true; f.Close();
        };
        timer.Start(); Pulsar(menu,boton);
        Comprobar(abierto,"F2 navegacion desde menu: "+boton);
    }
    private static void ProbarFase2(string cadena,string clave)
    {
        using(var scope=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(5)))
        using(var app=new Aplicacion(cadena))
        {
            app.Seguridad.Configurar("TEST-F2-G","Gerente","Fase II","test.f2.gerente",clave); app.Seguridad.Login("test.f2.gerente",clave);
            int cliente=app.Clientes.Guardar(new Cliente { TipoCliente="Particular",NombreRazonSocial="Cliente Fase II" });
            int proveedor=app.Proveedores.Guardar(new Proveedor { Nombre="Proveedor Fase II" });
            int marca=app.Catalogos.CrearMarca("Marca Fase II");
            int producto=app.Productos.Guardar(new Producto { CategoriaProductoID=app.Catalogos.Categorias().Single(c=>c.Nombre=="Pinturas").ID,MarcaID=marca,Nombre="Pintura Fase II",ManejaLote=true,AplicaPEPS=true });
            int presentacion=app.Fase2.Presentaciones().Single(p=>p.Nombre=="1 galon").ID;
            int color;
            using(var f=new ColorForm(app)) { Abrir(f); Texto(f,"Nombre","Azul de prueba"); Texto(f,"CodigoHex","#123456"); Pulsar(f,"Aceptar"); color=f.ColorCreadoID; Comprobar(color>0,"F2 color auxiliar desde formulario"); }
            int v1;
            using(var f=new ProductoVarianteForm(app))
            {
                Abrir(f); Elegir(f,"ProductoID",producto); Elegir(f,"PresentacionID",presentacion); Elegir(f,"ColorID",0); Texto(f,"CodigoSKU","TEST-F2-1"); Valor(f,"PrecioVentaActual",10); Valor(f,"StockMinimo",2); Pulsar(f,"Guardar");
                v1=app.Fase2.Variantes("TEST-F2-1").Single().ProductoVarianteID;
                Comprobar(app.Fase2.Variantes().Single().ColorID==null,"F2 crear variante con color opcional");
                SeleccionarPrimero(f); Valor(f,"StockMinimo",3); Pulsar(f,"Guardar"); Comprobar(app.Fase2.Variantes().Single().StockMinimo==3,"F2 modificar variante");
                SeleccionarPrimero(f); Pulsar(f,"Activar"); Comprobar(!app.Fase2.Variantes().Single().Activo,"F2 desactivar variante");
                SeleccionarPrimero(f); Pulsar(f,"Activar");
                Comprobar(Control<DataGridView>(f,"Tabla").Columns["PrecioVentaActual"]?.HeaderText=="Precio actual","F2 encabezados de variantes conservados tras recargar");
                Foto(f,"11-variantes");
            }
            int v2=app.Fase2.Guardar(new ProductoVariante { ProductoID=producto,PresentacionID=presentacion,ColorID=color,CodigoSKU="TEST-F2-2",PrecioVentaActual=20 });
            Rechaza(()=>app.Fase2.Guardar(new ProductoVariante { ProductoID=producto,PresentacionID=presentacion,CodigoSKU="OTRO",PrecioVentaActual=10 }),"F2 UNIQUE de combinacion sin color");
            int bodega,ubicacion;
            using(var f=new BodegaForm(app)) { Abrir(f); Texto(f,"Codigo","TEST-F2-B"); Texto(f,"Nombre","Bodega temporal"); Pulsar(f,"Guardar"); bodega=app.Fase2.Bodegas().Single().BodegaID; Comprobar(bodega>0,"F2 alta de bodega UI"); Foto(f,"12-bodegas"); }
            using(var f=new UbicacionForm(app)) { Abrir(f); Elegir(f,"BodegaID",bodega); Texto(f,"Codigo","A-01"); Pulsar(f,"Guardar"); ubicacion=app.Fase2.Ubicaciones().Single().UbicacionID; Comprobar(ubicacion>0,"F2 alta de ubicacion UI"); Foto(f,"13-ubicaciones"); }
            using(var f=new ProveedorProductoForm(app)) { Abrir(f); Elegir(f,"ProveedorID",proveedor); Elegir(f,"ProductoVarianteID",v1); Pulsar(f,"Guardar"); Comprobar(app.Fase2.Asociaciones().Count==1,"F2 asociacion proveedor-variante UI"); Foto(f,"14-proveedor-variante"); }
            app.Fase2.Guardar(new ProveedorProducto { ProveedorID=proveedor,ProductoVarianteID=v2 });
            Rechaza(()=>app.Fase2.Guardar(new ProveedorProducto { ProveedorID=proveedor,ProductoVarianteID=v1 }),"F2 rechazar asociacion duplicada");
            OrdenCompra orden;
            using(var f=new OrdenCompraForm(app))
            {
                Abrir(f); Texto(f,"Numero","TEST-F2-OC"); Elegir(f,"ProveedorID",proveedor);
                Elegir(f,"ProductoVarianteID",v1); Valor(f,"Cantidad",15); Valor(f,"CostoUnitario",4); Pulsar(f,"Agregar");
                Elegir(f,"ProductoVarianteID",v2); Valor(f,"Cantidad",10); Valor(f,"CostoUnitario",8); Pulsar(f,"Agregar");
                Foto(f,"15-orden-detalle"); Pulsar(f,"Guardar"); orden=app.Fase2.Ordenes().Single();
                Comprobar(orden.Estado=="PENDIENTE" && app.Fase2.OrdenDetalle(orden.OrdenCompraID).Count==2,"F2 orden de varios productos desde UI"); Foto(f,"16-orden-consulta");
            }
            var pendientes=app.Fase2.OrdenDetalle(orden.OrdenCompraID); int o1=pendientes.Single(l=>l.ProductoVarianteID==v1).DetalleOrdenCompraID,o2=pendientes.Single(l=>l.ProductoVarianteID==v2).DetalleOrdenCompraID;
            using(var f=new RecepcionCompraForm(app,orden))
            {
                Abrir(f); Texto(f,"Numero","TEST-F2-R1"); Control<DateTimePicker>(f,"FechaIngreso").Value=DateTime.Today.AddDays(-2);
                Elegir(f,"DetalleOrdenCompraID",o1); Elegir(f,"UbicacionID",ubicacion); Valor(f,"CantidadRecibida",5); Texto(f,"NumeroLoteProveedor","LOTE-A"); Pulsar(f,"Agregar");
                Elegir(f,"DetalleOrdenCompraID",o2); Valor(f,"CantidadRecibida",10); Texto(f,"NumeroLoteProveedor","LOTE-C"); Pulsar(f,"Agregar"); Foto(f,"17-recepcion-parcial"); Pulsar(f,"Guardar");
                Comprobar(app.Fase2.Ordenes().Single().Estado=="PARCIAL","F2 recepcion parcial desde UI");
            }
            Rechaza(()=>app.Fase2.Recibir("TEST-F2-EXCESO",orden.OrdenCompraID,DateTime.Today,[new LineaRecepcion { DetalleOrdenCompraID=o1,UbicacionID=ubicacion,CantidadRecibida=11,CostoUnitario=4,NumeroLoteProveedor="NO" }]),"F2 recepcion acumulada excesiva rechazada");
            Comprobar(app.Fase2.Recepciones(orden.OrdenCompraID).Count==2,"F2 fallo de recepcion no deja filas");
            using(var f=new RecepcionCompraForm(app,orden))
            {
                Abrir(f); Texto(f,"Numero","TEST-F2-R2"); Control<DateTimePicker>(f,"FechaIngreso").Value=DateTime.Today.AddDays(-1);
                Elegir(f,"DetalleOrdenCompraID",o1); Elegir(f,"UbicacionID",ubicacion); Valor(f,"CantidadRecibida",10); Valor(f,"CostoUnitario",5); Texto(f,"NumeroLoteProveedor","LOTE-B"); Pulsar(f,"Agregar"); Pulsar(f,"Guardar");
                Comprobar(app.Fase2.Ordenes().Single().Estado=="COMPLETA","F2 segunda recepcion completa orden"); Foto(f,"18-recepciones-consulta");
            }
            Comprobar(app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible)==15 && app.Fase2.Inventario().Single(x=>x.NumeroLoteProveedor=="LOTE-B").CostoUnitario==5,"F2 inventario conserva cantidades costo real y ubicacion");
            Cotizacion cot;
            using(var f=new CotizacionForm(app))
            {
                Abrir(f); Pulsar(f,"Agregar");
                Comprobar(Control<Label>(f,"Estado").Text.Contains("Seleccione una variante"),"F2 mensaje UI sin variante seleccionada");
                Texto(f,"Numero","TEST-F2-COT"); Elegir(f,"ClienteID",cliente);
                Elegir(f,"ProductoVarianteID",v1); Valor(f,"Cantidad",12); Pulsar(f,"Agregar");
                Elegir(f,"ProductoVarianteID",v2); Valor(f,"Cantidad",2); Pulsar(f,"Agregar");
                Comprobar(Control<Label>(f,"Estado").Text=="","F2 mensaje anterior se limpia al corregir captura");
                Foto(f,"19-cotizacion-detalle"); Texto(f,"Buscar","NO-COINCIDE"); Pulsar(f,"Guardar");
                Comprobar(Control<TextBox>(f,"Buscar").Text=="" && Control<DataGridView>(f,"Lista").CurrentRow?.DataBoundItem is Cotizacion,"F2 guardar limpia filtro anterior y selecciona cotizacion nueva");
                cot=app.Fase2.Cotizaciones().Single(); Comprobar(cot.Total==160 && cot.Estado=="PENDIENTE","F2 cotizacion total y precios sugeridos UI");
                Pulsar(f,"Confirmar"); cot=app.Fase2.Cotizaciones().Single(); Comprobar(cot.Estado=="CONFIRMADA","F2 confirmar cotizacion UI"); Foto(f,"20-cotizacion-confirmada");
            }
            var variante=app.Fase2.Variantes().Single(v=>v.ProductoVarianteID==v1); variante.PrecioVentaActual=99; app.Fase2.Guardar(variante);
            Factura factura;
            using(var f=new FacturacionForm(app,cot))
            {
                Abrir(f); Texto(f,"Numero","TEST-F2-F"); Comprobar(!Control<ComboBox>(f,"ClienteID").Enabled,"F2 cotizacion fija cliente y detalle historico"); Foto(f,"21-factura-desde-cotizacion"); Pulsar(f,"Guardar");
                factura=app.Fase2.Facturas().Single(); Comprobar(factura.TotalOriginal==160 && factura.CotizacionID==cot.CotizacionID,"F2 facturar cotizacion conserva precio historico"); Foto(f,"22-factura-consulta");
            }
            var inventario=app.Fase2.Inventario();
            Comprobar(inventario.Single(x=>x.NumeroLoteProveedor=="LOTE-A").CantidadDisponible==0 && inventario.Single(x=>x.NumeroLoteProveedor=="LOTE-B").CantidadDisponible==3,"F2 PEPS integra salidas de 5 y 7 de dos lotes");
            Rechaza(()=>app.Fase2.Facturar("TEST-F2-REPETIDA",cliente,[],cot.CotizacionID),"F2 cotizacion no reutilizable");
            Rechaza(()=>app.Fase2.Facturar("TEST-F2-SIN-STOCK",cliente,[new LineaVenta { ProductoVarianteID=v1,Cantidad=4,PrecioUnitario=10 }]),"F2 venta sin stock rechazada");
            Comprobar(app.Fase2.Facturas().Count==1 && app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible)==3,"F2 rollback de factura sin cambios parciales");
            using(var f=new FacturacionForm(app))
            {
                Abrir(f); Texto(f,"Numero","TEST-F2-DIRECTA"); Elegir(f,"ClienteID",cliente); Elegir(f,"ProductoVarianteID",v1); Valor(f,"Cantidad",1); Valor(f,"PrecioUnitario",10); Pulsar(f,"Agregar"); Pulsar(f,"Guardar");
                Comprobar(app.Fase2.Facturas().Single(x=>x.NumeroFactura=="TEST-F2-DIRECTA").CotizacionID==null,"F2 factura directa UI sin cotizacion artificial");
                var grid=Control<DataGridView>(f,"Lista");
                var fila=grid.Rows.Cast<DataGridViewRow>().Single(r=>r.DataBoundItem is Factura x && x.FacturaID==factura.FacturaID);
                grid.CurrentCell=fila.Cells.Cast<DataGridViewCell>().First(c=>c.Visible); Application.DoEvents();
                Comprobar(Control<DataGridView>(f,"DetalleConsulta").Rows.Count==2,"F2 seleccion de otra factura carga su propio detalle");
                Texto(f,"Buscar","TEST-F2-DIRECTA"); Pulsar(f,"Consultar");
                Comprobar(grid.Rows.Count==1 && Control<DataGridView>(f,"DetalleConsulta").Rows.Count==1,"F2 busqueda de factura filtra lista y detalle");
            }
            var facturaDirecta=app.Fase2.Facturas().Single(x=>x.NumeroFactura=="TEST-F2-DIRECTA");
            int empleado=app.Empleados.Guardar(new Empleado { CodigoEmpleado="TEST-F2-C",Nombres="Cajero",Apellidos="Temporal" });
            app.Seguridad.GuardarUsuario(new Usuario { EmpleadoID=empleado,RolID=app.Seguridad.Roles().Single(r=>r.Nombre=="Cajero").RolID,NombreUsuario="test.f2.cajero" },clave);
            empleado=app.Empleados.Guardar(new Empleado { CodigoEmpleado="TEST-F2-D",Nombres="Digitador",Apellidos="Temporal" });
            app.Seguridad.GuardarUsuario(new Usuario { EmpleadoID=empleado,RolID=app.Seguridad.Roles().Single(r=>r.Nombre=="Digitador").RolID,NombreUsuario="test.f2.digitador" },clave);
            int efectivo=app.Fase2.Medios().Single(m=>m.Nombre=="Efectivo").ID,tarjeta=app.Fase2.Medios().Single(m=>m.Nombre=="Tarjeta").ID;
            app.Seguridad.Login("test.f2.digitador",clave); Rechaza(()=>app.Fase2.Cobrar(factura.FacturaID,[new Pago { MedioPagoID=efectivo,Monto=160 }]),"F2 Digitador no cobra desde Negocio");
            app.Seguridad.Login("test.f2.cajero",clave); Rechaza(()=>app.Fase2.CrearOrden("PROHIBIDA",proveedor,[]),"F2 Cajero no crea compras desde Negocio");
            Rechaza(()=>app.Fase2.Facturar("PROHIBIDA",cliente,[]),"F2 Cajero no emite desde Negocio");
            Rechaza(()=>app.Fase2.Variantes(),"F2 Cajero no mantiene variantes");
            Rechaza(()=>app.Fase2.ReporteVentas(DateTime.Today,DateTime.Today),"Proyecto Final Cajero no consulta reportes Gerente");
            Rechaza(()=>app.Fase2.Devoluciones(),"Proyecto Final Cajero no consulta devoluciones Gerente");
            Rechaza(()=>app.Fase2.AnularFactura(facturaDirecta.FacturaID,"Motivo"),"Proyecto Final Cajero no anula facturas");
            Rechaza(()=>app.Fase2.CrearDevolucion("PROHIBIDA",factura.FacturaID,"Motivo",[]),"Proyecto Final Cajero no registra devoluciones");
            using(var f=new FacturacionForm(app)) { Abrir(f); Comprobar(f.Controls.OfType<TabControl>().Single().TabCount==1,"F2 Cajero solo consulta y cobra en UI"); Foto(f,"23-cajero-facturas"); }
            Rechaza(()=>app.Fase2.Cobrar(factura.FacturaID,[new Pago { MedioPagoID=efectivo,Monto=159 }]),"F2 suma de pagos incorrecta rechazada");
            using(var f=new PagoForm(app,factura))
            {
                Abrir(f); Elegir(f,"MedioPagoID",efectivo); Valor(f,"Monto",60); Pulsar(f,"Agregar");
                Elegir(f,"MedioPagoID",tarjeta); Valor(f,"Monto",100); Texto(f,"Referencia","TEST-REFERENCIA"); Pulsar(f,"Agregar"); Foto(f,"24-pagos-multiples"); Pulsar(f,"Cobrar");
                Comprobar(f.DialogResult==DialogResult.OK && app.Fase2.FacturaPagos(factura.FacturaID).Count==2,"F2 cobro completo con dos medios desde UI");
            }
            Rechaza(()=>app.Fase2.Cobrar(factura.FacturaID,[new Pago { MedioPagoID=efectivo,Monto=160 }]),"F2 cobro duplicado rechazado");
            factura=app.Fase2.Facturas().Single(x=>x.FacturaID==factura.FacturaID);
            var detalle=app.Fase2.FacturaDetalle(factura.FacturaID); var pagos=app.Fase2.FacturaPagos(factura.FacturaID);
            var texto=FacturaImpresion.Lineas(factura,detalle,pagos);
            Comprobar(texto.Any(x=>x.Contains("TOTAL Q 160")) && texto.Any(x=>x.Contains("Tarjeta")) && texto.Any(x=>x.Contains(factura.Emisor)),"F2 factura impresa incluye total medios y emisor");
            using(var documento=FacturaImpresion.Crear(factura,detalle,pagos))
            {
                var preview=new System.Drawing.Printing.PreviewPrintController(); documento.PrintController=preview;
                if(!documento.PrinterSettings.IsValid) throw new InvalidOperationException("Configure una impresora Windows valida para probar PrintDocument.");
                documento.Print(); var paginas=preview.GetPreviewPageInfo();
                Comprobar(paginas.Length==1,"F2 PrintDocument genera vista previa real sin enviar a impresora");
                foreach(var pagina in paginas) pagina.Image.Dispose();
            }
            using(var bitmap=new Bitmap(850,1100))
            using(var g=Graphics.FromImage(bitmap))
            { g.Clear(Color.White); int fin=FacturaImpresion.DibujarPagina(g,new Rectangle(45,45,760,1000),texto,0,1); Comprobar(fin==texto.Count,"F2 render de factura completo"); bitmap.Save(Path.Combine(Capturas,"25-factura-impresa.png")); }
            using(var bitmap=new Bitmap(850,1100))
            using(var g=Graphics.FromImage(bitmap))
            {
                var muchas=FacturaImpresion.Lineas(factura,Enumerable.Repeat(detalle[0],80),pagos); int inicio=0,paginas=0;
                while(inicio<muchas.Count) { int fin=FacturaImpresion.DibujarPagina(g,new Rectangle(45,45,760,1000),muchas,inicio,++paginas); if(fin<=inicio) throw new Exception("Impresion no avanza."); inicio=fin; }
                Comprobar(paginas>1 && inicio==muchas.Count,"F2 impresion de varias paginas sin perder lineas");
            }
            Comprobar(Fase2Servicio.Total([new LineaVenta { Cantidad=0.3333m,PrecioUnitario=1 },new LineaVenta { Cantidad=0.3333m,PrecioUnitario=1 }])==0.66m,"F2 redondeo por linea a dos decimales");
            app.Seguridad.Login("test.f2.gerente",clave);
            var lineaDevolucion=app.Fase2.DevolucionDisponibles(factura.FacturaID).First(x=>x.ProductoVarianteID==v1);
            decimal stockPrevio=app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible);
            int devolucionID=app.Fase2.CrearDevolucion("TEST-FINAL-DV",factura.FacturaID,"Prueba parcial desde Negocio",[new LineaDevolucion{DetalleFacturaLoteID=lineaDevolucion.DetalleFacturaLoteID,CantidadDevuelta=2}]);
            Comprobar(app.Fase2.DevolucionDetalle(devolucionID).Single().DetalleFacturaLoteID==lineaDevolucion.DetalleFacturaLoteID,"Proyecto Final devolución conserva lote de origen");
            Comprobar(app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible)==stockPrevio+2,"Proyecto Final devolución repone inventario");
            var excesoDevolucion=new LineaDevolucion{DetalleFacturaLoteID=lineaDevolucion.DetalleFacturaLoteID,CantidadDevuelta=lineaDevolucion.CantidadDisponible-1};
            Rechaza(()=>app.Fase2.CrearDevolucion("TEST-FINAL-DV-EXCESO",factura.FacturaID,"Exceso",[excesoDevolucion]),"Proyecto Final exceso acumulado en devolución rechazado");
            try
            {
                app.Fase2.CrearDevolucion("TEST-FINAL-DV-EXCESO-2",factura.FacturaID,"Exceso",[excesoDevolucion]);
                throw new Exception("Se esperaba el rechazo de la devolución excesiva.");
            }
            catch(InvalidOperationException ex)
            {
                Comprobar(ex.Message.Contains("excede lo vendido") && ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number==52121,
                    "Proyecto Final mensaje de validación conserva causa SQL para diagnóstico");
            }
            var reporteDesde=DateTime.Today.AddDays(-1);var reporteHasta=DateTime.Today;
            var resumenFinal=app.Fase2.ReporteVentas(reporteDesde,reporteHasta);
            var masVendidos=app.Fase2.ReporteProductos(reporteDesde,reporteHasta,"MAS_VENDIDOS",5);
            var ingresos=app.Fase2.ReporteProductos(reporteDesde,reporteHasta,"MAYORES_INGRESOS",10);
            var menosVendidos=app.Fase2.ReporteProductos(reporteDesde,reporteHasta,"MENOS_VENDIDOS",0);
            Comprobar(resumenFinal.CantidadFacturas==2&&resumenFinal.TotalFacturado==150,"Proyecto Final reporte fecha excluye devoluciones del subtotal histórico");
            Comprobar(masVendidos.Single(x=>x.ProductoVarianteID==v1).CantidadVendida==11&&ingresos.Any(x=>x.ProductoVarianteID==v1&&x.Ingresos==110),"Proyecto Final productos más vendidos e ingresos históricos");
            Comprobar(menosVendidos.Any(x=>x.ProductoVarianteID==v2),"Proyecto Final menos vendidos representa las variantes sin venta");
            _=app.Fase2.ReporteSinStock();
            var lotesFactura=app.Fase2.FacturaLotes(factura.FacturaID);
            Comprobar(lotesFactura.Any(x=>x.CantidadDevuelta==2&&x.CantidadNeta==lineaDevolucion.CantidadSalida-2),"Proyecto Final consulta factura presenta salida y devolución por lote");
            decimal stockAntesAnular=app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible);
            app.Fase2.AnularFactura(facturaDirecta.FacturaID,"Anulación de prueba desde Negocio");
            var facturaAnulada=app.Fase2.Facturas().Single(x=>x.FacturaID==facturaDirecta.FacturaID);
            Comprobar(facturaAnulada.Estado=="ANULADA"&&facturaAnulada.TotalOriginal==10&&facturaAnulada.TotalActual==0,"Proyecto Final anulación conserva el total original y pone actual en cero");
            Comprobar(app.Fase2.Inventario().Where(x=>x.ProductoVarianteID==v1).Sum(x=>x.CantidadDisponible)==stockAntesAnular+1,"Proyecto Final anulación repone al lote vendido");
            Rechaza(()=>app.Fase2.AnularFactura(facturaDirecta.FacturaID,"Anulación repetida"),"Proyecto Final doble anulación rechazada");
            var despuesAnular=app.Fase2.ReporteVentas(reporteDesde,reporteHasta);
            Comprobar(despuesAnular.CantidadFacturas==1&&despuesAnular.TotalFacturado==140,"Proyecto Final reporte excluye la factura anulada");
            using(var f=new InventarioForm(app)) { Abrir(f); Foto(f,"26-inventario"); Comprobar(Control<DataGridView>(f,"Tabla").Rows.Count==3,"F2 inventario por lote visible"); }
            using(var f=new MenuPrincipalForm(app)) { Abrir(f); Foto(f,"27-menu-fase2"); Comprobar(Control<Button>(f,"Facturas").Enabled && Control<Button>(f,"Cliente").Enabled,"F2 menu conserva Fase I e integra facturas");
                AbrirDesdeMenu<ProductoVarianteForm>(f,"Variantes"); AbrirDesdeMenu<CotizacionForm>(f,"Cotizaciones");
                AbrirDesdeMenu<OrdenCompraForm>(f,"Compras y recepciones"); AbrirDesdeMenu<InventarioForm>(f,"Inventario");
                AbrirDesdeMenu<FacturacionForm>(f,"Facturas");
            }
            Comprobar(app.Fase2.Inventario().All(x=>x.CantidadDisponible>=0),"F2 sin existencias negativas");
        }
        Comprobar(ConteoUsuarios(cadena)==0,"F2 rollback elimina cuentas y operaciones temporales");
    }
}
