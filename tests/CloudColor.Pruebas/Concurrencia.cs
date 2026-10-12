using System.Data;
using CloudColor.Entidades;
using CloudColor.Negocio;
using Microsoft.Data.SqlClient;

internal static partial class Program
{
    // Dos conexiones a la base desechable preparada por EntornoPruebas.
    private static void ProbarConcurrencia(string cadena,string clave)
    {
        var master=new SqlConnectionStringBuilder(cadena) { InitialCatalog="master",Pooling=false };
        using var admin=new SqlConnection(master.ConnectionString); admin.Open();
        using(var app=new Aplicacion(cadena))
        {
            app.Seguridad.Configurar("CONC-G","Gerente","Concurrencia","conc.gerente",clave); int actor=app.Seguridad.Login("conc.gerente",clave).UsuarioID;
            int cliente=app.Clientes.Guardar(new Cliente { TipoCliente="Particular",NombreRazonSocial="Prueba concurrente" });
            int proveedor=app.Proveedores.Guardar(new Proveedor { Nombre="Proveedor concurrente" });
            int marca=app.Catalogos.CrearMarca("Marca concurrente");
            int producto=app.Productos.Guardar(new Producto { Nombre="Producto concurrente",MarcaID=marca,CategoriaProductoID=app.Catalogos.Categorias()[0].ID,ManejaLote=false,AplicaPEPS=true });
            int variante=app.Fase2.Guardar(new ProductoVariante { ProductoID=producto,PresentacionID=app.Fase2.Presentaciones()[0].ID,CodigoSKU="CONC-1",PrecioVentaActual=10 });
            app.Fase2.Guardar(new ProveedorProducto { ProveedorID=proveedor,ProductoVarianteID=variante });
            int bodega=app.Fase2.Guardar(new Bodega { Codigo="CONC",Nombre="Bodega concurrente" });
            int ubicacion=app.Fase2.Guardar(new Ubicacion { BodegaID=bodega,Codigo="A" });
            int orden=app.Fase2.CrearOrden("CONC-OC",proveedor,[new LineaCompra { ProductoVarianteID=variante,CantidadSolicitada=15,CostoUnitarioOrden=4 }]);
            int linea=app.Fase2.OrdenDetalle(orden)[0].DetalleOrdenCompraID;
            app.Fase2.Recibir("CONC-R1",orden,DateTime.Today,[new LineaRecepcion { DetalleOrdenCompraID=linea,UbicacionID=ubicacion,CantidadRecibida=5,CostoUnitario=4 }]);
            app.Fase2.Recibir("CONC-R2",orden,DateTime.Today,[new LineaRecepcion { DetalleOrdenCompraID=linea,UbicacionID=ubicacion,CantidadRecibida=10,CostoUnitario=5 }]);
            var lotes=app.Fase2.Inventario().OrderBy(x=>x.LoteInventarioID).ToList();
            Comprobar(lotes.Count==2 && lotes.All(l=>l.NumeroLoteProveedor==null),"F2 existencia interna sin numero de lote proveedor");
            SqlCommand Venta(SqlConnection c,SqlTransaction? tx,string numero,int? cotizacion=null)
            {
                var cmd=new SqlCommand("dbo.sp_FacturaGenerar",c,tx) { CommandType=CommandType.StoredProcedure,CommandTimeout=15 };
                cmd.Parameters.Add("@UsuarioActorID",SqlDbType.Int).Value=actor; cmd.Parameters.Add("@ClienteID",SqlDbType.Int).Value=cliente;
                cmd.Parameters.Add("@NumeroFactura",SqlDbType.VarChar,30).Value=numero;
                cmd.Parameters.Add("@CotizacionID",SqlDbType.Int).Value=(object?)cotizacion ?? DBNull.Value;
                var t=new DataTable(); t.Columns.Add("ProductoVarianteID",typeof(int)); t.Columns.Add("Cantidad",typeof(decimal)); t.Columns.Add("PrecioUnitario",typeof(decimal));
                if(cotizacion==null) t.Rows.Add(variante,10m,10m);
                cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured) { TypeName="dbo.TipoDetalleFactura",Value=t }); return cmd;
            }
            Carrera(cadena,admin,(c,t)=>Venta(c,t,"CONC-F1"),(c,t)=>Venta(c,t,"CONC-F2"),52041,"ventas simultaneas no sobregiran stock");
            Comprobar(app.Fase2.Facturas().Count==1 && app.Fase2.Inventario().Sum(x=>x.CantidadDisponible)==5,"F2 solo una venta concurrente confirmada");
            var disponibles=app.Fase2.Inventario().OrderBy(x=>x.LoteInventarioID).ToList();
            Comprobar(disponibles[0].CantidadDisponible==0 && disponibles[1].CantidadDisponible==5,"F2 PEPS desempata igual FechaIngreso por LoteInventarioID");
            int cot=app.Fase2.CrearCotizacion("CONC-COT",cliente,[new LineaVenta { ProductoVarianteID=variante,Cantidad=1,PrecioUnitario=10 }]); app.Fase2.Confirmar(cot);
            Carrera(cadena,admin,(c,t)=>Venta(c,t,"CONC-CF1",cot),(c,t)=>Venta(c,t,"CONC-CF2",cot),52040,"cotizacion concurrente genera maximo una factura");
            int factura=app.Fase2.Facturas().Single(f=>f.NumeroFactura=="CONC-F1").FacturaID;
            int efectivo=app.Fase2.Medios().Single(m=>m.Nombre=="Efectivo").ID;
            SqlCommand Cobro(SqlConnection c,SqlTransaction? tx)
            {
                var cmd=new SqlCommand("dbo.sp_FacturaCobrar",c,tx) { CommandType=CommandType.StoredProcedure,CommandTimeout=15 };
                cmd.Parameters.Add("@UsuarioActorID",SqlDbType.Int).Value=actor; cmd.Parameters.Add("@FacturaID",SqlDbType.Int).Value=factura;
                var t=new DataTable(); t.Columns.Add("MedioPagoID",typeof(int)); t.Columns.Add("Monto",typeof(decimal)); t.Columns.Add("Referencia",typeof(string)); t.Rows.Add(efectivo,100m,DBNull.Value);
                cmd.Parameters.Add(new SqlParameter("@Pagos",SqlDbType.Structured) { TypeName="dbo.TipoPagoFactura",Value=t }); return cmd;
            }
            Carrera(cadena,admin,Cobro,Cobro,52042,"cobros concurrentes no duplican pagos");
            Comprobar(app.Fase2.FacturaPagos(factura).Count==1,"F2 un solo pago persistido tras doble cobro");
            int orden2=app.Fase2.CrearOrden("CONC-OC2",proveedor,[new LineaCompra { ProductoVarianteID=variante,CantidadSolicitada=10,CostoUnitarioOrden=4 }]);
            int linea2=app.Fase2.OrdenDetalle(orden2)[0].DetalleOrdenCompraID;
            SqlCommand Recibir(SqlConnection c,SqlTransaction? tx,string numero)
            {
                var cmd=new SqlCommand("dbo.sp_RecepcionCompraRegistrar",c,tx) { CommandType=CommandType.StoredProcedure,CommandTimeout=15 };
                cmd.Parameters.Add("@UsuarioActorID",SqlDbType.Int).Value=actor; cmd.Parameters.Add("@OrdenCompraID",SqlDbType.Int).Value=orden2;
                cmd.Parameters.Add("@NumeroRecepcion",SqlDbType.VarChar,30).Value=numero; cmd.Parameters.Add("@FechaIngreso",SqlDbType.DateTime).Value=DateTime.Today;
                var t=new DataTable(); t.Columns.Add("DetalleOrdenCompraID",typeof(int)); t.Columns.Add("CantidadRecibida",typeof(decimal)); t.Columns.Add("CostoUnitario",typeof(decimal)); t.Columns.Add("UbicacionID",typeof(int)); t.Columns.Add("NumeroLoteProveedor",typeof(string)); t.Columns.Add("FechaVencimiento",typeof(DateTime));
                t.Rows.Add(linea2,7m,4m,ubicacion,DBNull.Value,DBNull.Value);
                cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured) { TypeName="dbo.TipoDetalleRecepcion",Value=t }); return cmd;
            }
            Carrera(cadena,admin,(c,t)=>Recibir(c,t,"CONC-R3"),(c,t)=>Recibir(c,t,"CONC-R4"),52051,"recepciones concurrentes respetan cantidad solicitada");
            Comprobar(app.Fase2.OrdenDetalle(orden2)[0].CantidadRecibida==7 && app.Fase2.OrdenDetalle(orden2)[0].CantidadPendiente==3,"F2 una recepcion concurrente y saldo pendiente correcto");
        }
    }
    private static void Carrera(string cadena,SqlConnection admin,Func<SqlConnection,SqlTransaction?,SqlCommand> primera,Func<SqlConnection,SqlTransaction?,SqlCommand> segunda,int errorEsperado,string titulo)
    {
        using var uno=new SqlConnection(cadena); uno.Open();
        using var dos=new SqlConnection(cadena); dos.Open();
        using var tx=uno.BeginTransaction(); using(var cmd=primera(uno,tx)) cmd.ExecuteNonQuery();
        int sesion1=uno.ServerProcessId,sesion2=dos.ServerProcessId;
        var otra=Task.Run(()=> { try { using var cmd=segunda(dos,null); cmd.ExecuteNonQuery(); return 0; } catch(SqlException ex) { return ex.Number; } });
        bool bloqueada=false;
        try
        {
            DateTime limite=DateTime.UtcNow.AddSeconds(5);
            while(DateTime.UtcNow<limite && !otra.IsCompleted)
            {
                using var estado=new SqlCommand("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id=@Segunda AND blocking_session_id=@Primera",admin);
                estado.Parameters.Add("@Segunda",SqlDbType.Int).Value=sesion2; estado.Parameters.Add("@Primera",SqlDbType.Int).Value=sesion1;
                if((int)estado.ExecuteScalar()!>0) { bloqueada=true; break; }
                Thread.Sleep(50);
            }
        }
        finally { tx.Commit(); }
        int error=otra.GetAwaiter().GetResult();
        Comprobar(bloqueada && error==errorEsperado,"F2 concurrencia: "+titulo+" (bloqueo observado en SQL Server)");
    }
}
