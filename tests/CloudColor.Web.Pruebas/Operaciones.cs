using System.Net;
using CloudColor.Negocio;
using CloudColor.Entidades;
internal static partial class Program
{
    private static async Task Exito(HttpClient c,string pagina,string ruta,Dictionary<string,string> datos,string nombre)
    {
        using var r=await Post(c,pagina,ruta,datos);Check(r.StatusCode==HttpStatusCode.Redirect,nombre+" ("+(int)r.StatusCode+")");
    }
    private static async Task ProbarOperaciones(HttpClient c,HttpClient cajero,string cadena)
    {
        using var a=new Aplicacion(cadena);a.Seguridad.Login("web.gerente",Clave);
        await Exito(c,"/Proveedores/Editar","/Proveedores/Editar/0",new(){{"Nombre","Proveedor QA web"}},"alta proveedor");int proveedor=a.Proveedores.Listar().Single().ProveedorID;
        await Exito(c,"/Productos","/Productos/Marca",new(){{"nombre","Marca QA web"}},"marca auxiliar");int marca=a.Catalogos.Marcas().Single().ID;
        await Exito(c,"/Productos/Editar","/Productos/Editar/0",new(){{"Nombre","Pintura QA web"},{"CategoriaProductoID",a.Catalogos.Categorias().Single(x=>x.Nombre=="Pinturas").ID.ToString()},{"MarcaID",marca.ToString()},{"ManejaLote","true"},{"AplicaPEPS","true"}},"alta producto");int producto=a.Productos.Listar().Single().ProductoID;
        await Exito(c,"/Variantes/Editar","/Variantes/Editar/0",new(){{"ProductoID",producto.ToString()},{"PresentacionID",a.Fase2.Presentaciones().Single(x=>x.Nombre=="1 galon").ID.ToString()},{"CodigoSKU","WEB-QA-1"},{"PrecioVentaActual","20.00"},{"StockMinimo","5"}},"alta variante decimal");int v=a.Fase2.Variantes().Single().ProductoVarianteID;
        await Exito(c,"/Bodegas/Editar","/Bodegas/Editar/0",new(){{"Codigo","WEB-B1"},{"Nombre","Bodega QA web"}},"alta bodega");int b=a.Fase2.Bodegas().Single().BodegaID;
        await Exito(c,"/Ubicaciones/Editar","/Ubicaciones/Editar/0",new(){{"BodegaID",b.ToString()},{"Codigo","A-01"},{"Descripcion","Ubicacion QA"}},"alta ubicacion");int u=a.Fase2.Ubicaciones().Single().UbicacionID;
        await Exito(c,"/ProveedorProducto/Editar","/ProveedorProducto/Editar/0",new(){{"ProveedorID",proveedor.ToString()},{"ProductoVarianteID",v.ToString()}},"asociacion proveedor variante");
        var compra=new Dictionary<string,string>{{"Numero","WEB-OC-100"},{"PersonaId",proveedor.ToString()},{"accion","Guardar"},{"Detalle[0].ProductoVarianteID",v.ToString()},{"Detalle[0].Cantidad","100"},{"Detalle[0].PrecioUnitario","9.50"}};
        await Exito(c,"/Compras/Nueva","/Compras/Nueva",compra,"orden HTTP de 100 unidades");int orden=a.Fase2.Ordenes().Single().OrdenCompraID;int linea=a.Fase2.OrdenDetalle(orden).Single().DetalleOrdenCompraID;
        Dictionary<string,string> Rec(string numero,string cantidad)=>new(){{"Numero",numero},{"FechaIngreso",DateTime.Today.ToString("yyyy-MM-dd")},{"Lineas[0].Incluir","true"},{"Lineas[0].DetalleOrdenCompraID",linea.ToString()},{"Lineas[0].CantidadRecibida",cantidad},{"Lineas[0].CostoUnitario","10.25"},{"Lineas[0].UbicacionID",u.ToString()},{"Lineas[0].NumeroLoteProveedor","QA-LOTE-"+numero}};
        await Exito(c,"/Recepciones/Nueva/"+orden,"/Recepciones/Nueva/"+orden,Rec("WEB-R75","75"),"recepcion HTTP 75");Check(a.Fase2.Ordenes().Single().Estado=="PARCIAL","orden parcial 75/100");
        using(var r=await Post(c,"/Recepciones/Nueva/"+orden,"/Recepciones/Nueva/"+orden,Rec("WEB-EXCESO","30")))Check(r.StatusCode==HttpStatusCode.OK&&a.Fase2.OrdenDetalle(orden).Single().CantidadRecibida==75,"sobre-recepcion HTTP rechazada sin cambios");
        await Exito(c,"/Recepciones/Nueva/"+orden,"/Recepciones/Nueva/"+orden,Rec("WEB-R25","25"),"recepcion HTTP 25");Check(a.Fase2.Ordenes().Single().Estado=="COMPLETA","orden completa 100/100");
        int cliente=a.Clientes.Listar().Single().ClienteID;
        Dictionary<string,string> Venta(string numero,string cantidad)=>new(){{"Numero",numero},{"PersonaId",cliente.ToString()},{"accion","Guardar"},{"Detalle[0].ProductoVarianteID",v.ToString()},{"Detalle[0].Cantidad",cantidad},{"Detalle[0].PrecioUnitario","20.00"}};
        await Exito(c,"/Cotizaciones/Nueva","/Cotizaciones/Nueva",Venta("WEB-COT-1","12"),"cotizacion HTTP");int cot=a.Fase2.Cotizaciones().Single().CotizacionID;
        await Exito(c,"/Cotizaciones/Ver/"+cot,"/Cotizaciones/Confirmar/"+cot,new(),"confirmar cotizacion HTTP");
        var variante=a.Fase2.Variantes().Single();variante.PrecioVentaActual=29;a.Fase2.Guardar(variante);
        await Exito(c,"/Facturas/Nueva?cotizacion="+cot,"/Facturas/Nueva",new(){{"Numero","WEB-F-COT"},{"PersonaId",cliente.ToString()},{"CotizacionId",cot.ToString()},{"accion","Guardar"}},"factura desde cotizacion HTTP");int factura=a.Fase2.Facturas().Single().FacturaID;
        Check(a.Fase2.Facturas().Single().TotalOriginal==240,"precio historico tras cambiar precio actual");Check(a.Fase2.Inventario().OrderBy(x=>x.LoteInventarioID).First().CantidadDisponible==63,"PEPS resuelto en SQL desde web");
        await Exito(c,"/Facturas/Nueva","/Facturas/Nueva",Venta("WEB-F-DIRECTA","1"),"factura directa HTTP");Check(a.Fase2.Facturas().Single(x=>x.NumeroFactura=="WEB-F-DIRECTA").CotizacionID==null,"venta directa sin cotizacion artificial");
        int ef=a.Fase2.Medios().Single(x=>x.Nombre=="Efectivo").ID,tar=a.Fase2.Medios().Single(x=>x.Nombre=="Tarjeta").ID;
        var pagos=new Dictionary<string,string>{{"accion","Cobrar"},{"Pagos[0].MedioPagoID",ef.ToString()},{"Pagos[0].Monto","100"},{"Pagos[1].MedioPagoID",tar.ToString()},{"Pagos[1].Monto","140"},{"Pagos[1].Referencia","QA-TARJETA"}};
        await Exito(cajero,"/Cobros/Registrar/"+factura,"/Cobros/Registrar/"+factura,pagos,"Cajero cobra efectivo y tarjeta HTTP");Check(a.Fase2.FacturaPagos(factura).Count==2,"dos pagos persistidos");
        using(var r=await Post(cajero,"/Cobros/Registrar/"+factura,"/Cobros/Registrar/"+factura,pagos))Check(r.StatusCode==HttpStatusCode.OK&&a.Fase2.FacturaPagos(factura).Count==2,"cobro repetido rechazado");
        foreach(string ruta in new[]{"/Dashboard","/Inventario","/Compras/Ver/"+orden,"/Recepciones/Ver/"+orden,"/Cotizaciones/Ver/"+cot,"/Facturas/Ver/"+factura,"/Facturas/Imprimir/"+factura,"/Cobros","/Usuarios","/Roles","/Empleados"})await Pagina(c,ruta);
        using var digitador=Cliente();await Login(digitador,"web.digitador");
        using(var r=await Post(digitador,"/Dashboard","/Usuarios/Editar/0",new(){{"NombreUsuario","intruso"}}))Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccesoDenegado"),"Digitador no crea usuarios por POST directo");
        using(var r=await Post(cajero,"/Dashboard","/Productos/Editar/0",new(){{"Nombre","intruso"}}))Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccesoDenegado"),"Cajero no crea productos por POST directo");
        string usuariosHtml=await Pagina(c,"/Usuarios");Check(!usuariosHtml.Contains("PasswordHash")&&!usuariosHtml.Contains("PBKDF2$"),"administracion no expone verificadores");
        await Exito(c,"/Proveedores/Editar/"+proveedor,"/Proveedores/Editar/"+proveedor,new(){{"Nombre","Proveedor QA web editado"}},"editar proveedor HTTP");
        Check(a.Proveedores.Listar("editado").Single().ProveedorID==proveedor,"busqueda y edicion de proveedor persistidas");
        await Exito(c,"/Productos/Editar/"+producto,"/Productos/Editar/"+producto,new(){{"Nombre","Pintura QA web"},{"CategoriaProductoID",a.Productos.Listar().Single().CategoriaProductoID.ToString()},{"MarcaID",marca.ToString()},{"Descripcion","Descripcion editada"},{"ManejaLote","true"},{"AplicaPEPS","true"}},"editar producto sin alterar variante");
        Check(a.Productos.Listar().Single().Descripcion=="Descripcion editada"&&a.Fase2.Variantes().Single().PrecioVentaActual==29,"producto y variante conservan sus responsabilidades");
        await Exito(c,"/Empleados/Editar","/Empleados/Editar/0",new(){{"CodigoEmpleado","WEB-ADMIN"},{"Nombres","Empleado"},{"Apellidos","Temporal"}},"alta empleado HTTP");
        int empleado=a.Empleados.Listar().Single(x=>x.CodigoEmpleado=="WEB-ADMIN").EmpleadoID;
        await Exito(c,"/Empleados/Editar/"+empleado,"/Empleados/Editar/"+empleado,new(){{"CodigoEmpleado","WEB-ADMIN"},{"Nombres","Empleado editado"},{"Apellidos","Temporal"}},"editar empleado HTTP");
        int rolDigitador=a.Seguridad.Roles().Single(x=>x.Nombre=="Digitador").RolID;
        await Exito(c,"/Usuarios/Editar","/Usuarios/Editar/0",new(){{"NombreUsuario","web.temporal"},{"EmpleadoID",empleado.ToString()},{"RolID",rolDigitador.ToString()},{"Password",Clave}},"alta usuario HTTP reutiliza PBKDF2");
        int cuenta=a.Seguridad.Usuarios().Single(x=>x.NombreUsuario=="web.temporal").UsuarioID;
        await Exito(c,"/Usuarios/Editar/"+cuenta,"/Usuarios/Editar/"+cuenta,new(){{"NombreUsuario","web.temporal"},{"EmpleadoID",empleado.ToString()},{"RolID",rolDigitador.ToString()},{"Password",""}},"editar usuario sin reemplazar contraseña");
        using(var temporal=Cliente())await Login(temporal,"web.temporal");
        await Exito(c,"/Roles/Editar/"+rolDigitador,"/Roles/Editar/"+rolDigitador,new(){{"Nombre","Rol inventado"},{"Descripcion","Descripcion QA"},{"Activo","true"}},"editar descripcion de rol HTTP");
        Check(a.Seguridad.Roles().Single(x=>x.RolID==rolDigitador).Nombre=="Digitador"&&a.Seguridad.Roles().Count==3,"nombre inmutable y exactamente tres roles");
        foreach(string ruta in new[]{"/Usuarios","/Roles","/Cobros/Registrar/"+factura}){using var r=await digitador.GetAsync(ruta);Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccesoDenegado"),"Digitador bloqueado en "+ruta);}
        foreach(string ruta in new[]{"/Compras","/Inventario","/Facturas/Nueva"}){using var r=await cajero.GetAsync(ruta);Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccesoDenegado"),"Cajero bloqueado en "+ruta);}
        var usuario=a.Seguridad.Usuarios().Single(x=>x.NombreUsuario=="web.digitador");a.Seguridad.ActivarUsuario(usuario.UsuarioID,false);
        using(var r=await digitador.GetAsync("/Dashboard"))Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("Login"),"cuenta desactivada revoca cookie en siguiente solicitud");a.Seguridad.ActivarUsuario(usuario.UsuarioID,true);
    }
}
