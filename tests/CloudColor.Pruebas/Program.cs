using System.Transactions;
using CloudColor.Datos;
using CloudColor.Entidades;
using CloudColor.Negocio;
using CloudColor.Presentacion;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static int pruebas;
    private static string Capturas => Path.Combine(Path.GetTempPath(),"CloudColor-Pruebas");
    private static void Comprobar(bool condicion,string nombre)
    {
        if(!condicion) throw new Exception("Fallo: "+nombre);
        pruebas++; Console.WriteLine("OK: "+nombre);
    }
    private static void Rechaza(Action accion,string nombre)
    {
        bool rechazo=false;
        try { accion(); } catch(Exception ex) when(ex is ArgumentException or InvalidOperationException) { rechazo=true; }
        Comprobar(rechazo,nombre);
    }
    private static T Control<T>(Form form,string nombre) where T:System.Windows.Forms.Control =>
        (T)form.Controls.Find(nombre,true).Single();
    private static void Abrir(Form f)
    {
        f.ShowInTaskbar=false; f.StartPosition=FormStartPosition.Manual; f.Location=new Point(-20000,-20000);
        f.Show(); Application.DoEvents();
    }
    private static void Pulsar(Form f,string nombre) { Control<Button>(f,nombre).PerformClick(); Application.DoEvents(); }
    private static void Texto(Form f,string nombre,string valor) => Control<TextBox>(f,nombre).Text=valor;
    private static void NuevaMarca(ProductoForm producto,string? nombre)
    {
        Exception? error=null;
        bool completado=false;
        var limite=DateTime.UtcNow.AddSeconds(10);
        using var timer=new System.Windows.Forms.Timer { Interval=50 };
        timer.Tick+=(_,_)=>
        {
            var dialogo=Application.OpenForms.OfType<MarcaForm>().SingleOrDefault();
            if(dialogo==null && DateTime.UtcNow<limite) return;
            timer.Stop();
            try
            {
                if(dialogo==null) throw new Exception("No se abrio Nueva marca.");
                dialogo.ShowInTaskbar=false; dialogo.Location=new Point(-20000,-20000);
                if(nombre==null) dialogo.Close();
                else
                {
                    Texto(dialogo,"Nombre",nombre); Pulsar(dialogo,"Aceptar");
                    if(dialogo.DialogResult!=DialogResult.OK) throw new Exception("No se guardo Nueva marca.");
                }
                completado=true;
            }
            catch(Exception ex) { error=ex; dialogo?.Close(); }
        };
        timer.Start();
        Pulsar(producto,"NuevaMarca");
        if(error!=null) throw error;
        Comprobar(completado,nombre==null ? "cancelar dialogo Nueva marca" : "crear marca desde dialogo modal de Producto");
    }
    private static void Foto(Form f,string nombre)
    {
        Directory.CreateDirectory(Capturas);
        using var bitmap=new Bitmap(f.Width,f.Height); f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));
        bitmap.Save(Path.Combine(Capturas,nombre+".png"));
    }
    private static int ConteoUsuarios(string cadena)
    {
        using var c=new SqlConnection(cadena); c.Open(); using var cmd=new SqlCommand("SELECT COUNT(*) FROM dbo.Usuario",c);
        return (int)cmd.ExecuteScalar()!;
    }
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        try
        {
            string cadena=Conexion.LeerCadena();
            Comprobar(ConteoUsuarios(cadena)==0,"base sin usuarios para probar configuracion inicial; nunca elimina cuentas existentes");
            string clave=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            string h1=PasswordHasher.Crear(clave),h2=PasswordHasher.Crear(clave);
            Comprobar(h1!=h2 && h1.Length<=255,"salt aleatorio y formato compatible con PasswordHash");
            Comprobar(PasswordHasher.Verificar(clave,h1),"PBKDF2 correcto");
            Comprobar(!PasswordHasher.Verificar(clave+"x",h1),"PBKDF2 rechaza contraseña incorrecta");
            Comprobar(!PasswordHasher.Verificar(clave,"PBKDF2$SHA256$999999999$xx$xx"),"hash malformado o costo fuera de limite rechazado");
            Rechaza(()=>PasswordHasher.Crear("corta"),"contraseña minima");
            using(var scope=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(5)))
            using(var app=new Aplicacion(cadena))
            {
                Comprobar(app.Seguridad.InicialPendiente(),"configuracion pendiente");
                using(var inicial=new ConfiguracionInicialForm(app))
                {
                    Abrir(inicial); Foto(inicial,"01-configuracion");
                    Texto(inicial,"CodigoEmpleado","TEST-GERENTE"); Texto(inicial,"Nombres","Gerente"); Texto(inicial,"Apellidos","Temporal");
                    Texto(inicial,"NombreUsuario","test.gerente"); Texto(inicial,"Password",clave); Texto(inicial,"Confirmar",clave); Pulsar(inicial,"Aceptar");
                    Comprobar(inicial.DialogResult==DialogResult.OK,"formulario crea primer empleado y gerente");
                }
                Comprobar(!app.Seguridad.InicialPendiente(),"configuracion inicial deja de ofrecerse");
                using(var login=new LoginForm(app))
                {
                    Abrir(login); Texto(login,"NombreUsuario","test.gerente"); Texto(login,"Password","incorrecta"); Pulsar(login,"Aceptar");
                    Comprobar(login.DialogResult!=DialogResult.OK && Control<TextBox>(login,"Password").Text=="","login invalido limpia contraseña");
                    Foto(login,"02-login"); Texto(login,"Password",clave); Pulsar(login,"Aceptar");
                    Comprobar(login.DialogResult==DialogResult.OK,"login de gerente desde formulario");
                }
                using(var menu=new MenuPrincipalForm(app)) { Abrir(menu); Comprobar(Control<Button>(menu,"Usuarios").Enabled,"menu gerente habilitado"); Foto(menu,"03-menu"); }
                Rechaza(()=>app.Clientes.Guardar(new Cliente()),"campos obligatorios en negocio");
                using(var f=new ClienteForm(app))
                {
                    Abrir(f); Control<ComboBox>(f,"TipoCliente").SelectedItem="Particular"; Texto(f,"NombreRazonSocial","Cliente UI"); Pulsar(f,"Guardar");
                    var cliente=app.Clientes.Listar("Cliente UI").Single(); Comprobar(cliente.Activo,"crear Cliente desde formulario");
                    SeleccionarPrimero(f); Texto(f,"NombreRazonSocial","Cliente UI editado"); Pulsar(f,"Guardar");
                    Comprobar(app.Clientes.Listar("editado").Count==1,"editar Cliente desde formulario");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Comprobar(!app.Clientes.Listar("editado").Single().Activo,"desactivar Cliente desde formulario");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Foto(f,"04-clientes");
                }
                using(var f=new ProveedorForm(app))
                {
                    Abrir(f); Texto(f,"Nombre","Proveedor UI"); Pulsar(f,"Guardar"); Comprobar(app.Proveedores.Listar().Count==1,"crear Proveedor UI");
                    SeleccionarPrimero(f); Texto(f,"Nombre","Proveedor editado"); Pulsar(f,"Guardar"); Comprobar(app.Proveedores.Listar("editado").Count==1,"editar Proveedor UI");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Comprobar(!app.Proveedores.Listar().Single().Activo,"desactivar Proveedor UI"); Foto(f,"05-proveedores");
                }
                int marca=app.Catalogos.CrearMarca("Marca temporal");
                using(var f=new ProductoForm(app))
                {
                    Abrir(f); Control<ComboBox>(f,"CategoriaProductoID").SelectedIndex=0; Control<ComboBox>(f,"MarcaID").SelectedValue=marca;
                    Texto(f,"Nombre","Producto UI"); Pulsar(f,"Guardar"); Comprobar(app.Productos.Listar().Count==1,"crear Producto UI con catalogos");
                    int productoID=app.Productos.Listar().Single().ProductoID;
                    SeleccionarPrimero(f);
                    var categorias=Control<ComboBox>(f,"CategoriaProductoID");
                    categorias.SelectedIndex=1; // Una categoria distinta de la primera detecta el reinicio del ComboBox.
                    int categoriaID=(int)categorias.SelectedValue!;
                    Texto(f,"Nombre","Producto editado"); Texto(f,"Descripcion","Descripcion sin guardar");
                    Control<CheckBox>(f,"ManejaLote").Checked=true; Control<CheckBox>(f,"AplicaPEPS").Checked=true;
                    NuevaMarca(f,null);
                    Comprobar((int)categorias.SelectedValue! == categoriaID && (int)Control<ComboBox>(f,"MarcaID").SelectedValue! == marca,
                        "cancelar Nueva marca conserva categoria y marca del producto en edicion");
                    NuevaMarca(f,"ZZ Marca nueva del editor"); // No es la primera marca por orden alfabetico.
                    int marcaNueva=app.Catalogos.Marcas().Single(m=>m.Nombre=="ZZ Marca nueva del editor").ID;
                    Comprobar((int)categorias.SelectedValue! == categoriaID,"Nueva marca restaura categoria no inicial");
                    Comprobar((int)Control<ComboBox>(f,"MarcaID").SelectedValue! == marcaNueva,"Nueva marca selecciona explicitamente el ID creado");
                    Comprobar(Control<TextBox>(f,"Nombre").Text=="Producto editado" && Control<TextBox>(f,"Descripcion").Text=="Descripcion sin guardar"
                        && Control<CheckBox>(f,"ManejaLote").Checked && Control<CheckBox>(f,"AplicaPEPS").Checked,
                        "Nueva marca conserva nombre descripcion lote y PEPS sin guardar");
                    Foto(f,"06-producto-nueva-marca");
                    Pulsar(f,"Guardar"); Comprobar(app.Productos.Listar("editado").Count==1,"editar Producto UI");
                    var guardado=app.Productos.Listar().Single();
                    Comprobar(guardado.ProductoID==productoID && guardado.CategoriaProductoID==categoriaID && guardado.MarcaID==marcaNueva
                        && guardado.Nombre=="Producto editado" && guardado.Descripcion=="Descripcion sin guardar" && guardado.ManejaLote && guardado.AplicaPEPS,
                        "guardar tras Nueva marca actualiza el mismo Producto con todos sus valores");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Comprobar(!app.Productos.Listar().Single().Activo,"desactivar Producto UI"); Foto(f,"06-productos");
                }
                int empleado;
                using(var f=new EmpleadoForm(app))
                {
                    Abrir(f); Texto(f,"CodigoEmpleado","TEST-OTRO"); Texto(f,"Nombres","Otro"); Texto(f,"Apellidos","Temporal"); Pulsar(f,"Guardar");
                    empleado=app.Empleados.Listar("Otro").Single().EmpleadoID; Comprobar(empleado>0,"crear Empleado UI");
                    Texto(f,"Buscar","Otro"); Pulsar(f,"Consultar"); SeleccionarPrimero(f); Texto(f,"Nombres","Otro editado"); Pulsar(f,"Guardar");
                    Comprobar(app.Empleados.Listar("editado").Count==1,"editar Empleado UI");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Comprobar(!app.Empleados.Listar("Otro").Single().Activo,"desactivar Empleado UI");
                    SeleccionarPrimero(f); Pulsar(f,"Activar"); Foto(f,"07-empleados");
                }
                int digitador=app.Seguridad.Roles().Single(r=>r.Nombre=="Digitador").RolID;
                int cajero=app.Seguridad.Roles().Single(r=>r.Nombre=="Cajero").RolID;
                using(var f=new UsuariosForm(app))
                {
                    Abrir(f); Control<ComboBox>(f,"EmpleadoID").SelectedValue=empleado; Control<ComboBox>(f,"RolID").SelectedValue=digitador;
                    Texto(f,"NombreUsuario","test.otro"); Texto(f,"Password",clave); Pulsar(f,"Guardar");
                    Comprobar(app.Seguridad.Usuarios().Count==2,"crear usuario UI");
                    Comprobar(!Control<DataGridView>(f,"Tabla").Columns.Contains("PasswordHash"),"grid de usuarios sin PasswordHash");
                    Texto(f,"Buscar","test.otro"); Pulsar(f,"Consultar"); SeleccionarPrimero(f); Texto(f,"NombreUsuario","test.editado"); Pulsar(f,"Guardar");
                    Comprobar(app.Seguridad.Usuarios().Any(u=>u.NombreUsuario=="test.editado"),"editar usuario sin reemplazar hash"); Foto(f,"08-usuarios");
                }
                var otro=app.Seguridad.Usuarios().Single(u=>u.NombreUsuario=="test.editado");
                using(var f=new RolesForm(app))
                {
                    Abrir(f); Texto(f,"Buscar","Digitador"); Pulsar(f,"Consultar"); SeleccionarPrimero(f); Texto(f,"Descripcion","Descripcion de prueba"); Pulsar(f,"Guardar");
                    Comprobar(app.Seguridad.Roles().Single(r=>r.RolID==digitador).Descripcion=="Descripcion de prueba","editar Rol UI"); Foto(f,"09-roles");
                }
                app.Seguridad.Login("test.editado",clave);
                Comprobar(app.Clientes.Listar().Count==1,"digitador accede a mantenimiento");
                Rechaza(()=>app.Seguridad.Usuarios(),"digitador no administra usuarios");
                using(var f=new MenuPrincipalForm(app)) { Abrir(f); Comprobar(!Control<Button>(f,"Usuarios").Enabled && Control<Button>(f,"Cliente").Enabled,"menu digitador"); }
                app.Seguridad.Login("test.gerente",clave); otro.RolID=cajero; app.Seguridad.GuardarUsuario(otro,null);
                app.Seguridad.Login("test.editado",clave); Rechaza(()=>app.Clientes.Listar(),"cajero no accede a negocio de mantenimiento");
                using(var f=new MenuPrincipalForm(app)) { Abrir(f); Comprobar(!Control<Button>(f,"Cliente").Enabled && !Control<Button>(f,"Roles").Enabled,"menu cajero"); Foto(f,"10-cajero"); }
                app.Seguridad.Login("test.gerente",clave); app.Seguridad.ActivarUsuario(otro.UsuarioID,false);
                Rechaza(()=>app.Seguridad.Login("test.editado",clave),"login de cuenta inactiva rechazado");
                app.Seguridad.Login("test.gerente",clave);
                Rechaza(()=>app.Seguridad.ActivarUsuario(app.Seguridad.Actual().UsuarioID,false),"no desactivar propia cuenta Gerente");
                // Sin Complete: se revierten todos los registros, incluidos los creados por formularios.
            }
            Comprobar(ConteoUsuarios(cadena)==0,"rollback sin cuentas ni contraseñas de prueba persistidas");
            using(var scope=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(1)))
            using(var db=new AccesoDatos(cadena))
            {
                var gerente=new SeguridadServicio(new SeguridadDatos(db));
                var colaborador=new SeguridadServicio(new SeguridadDatos(db));
                gerente.Configurar("TEST-SESION","Gerente","Temporal","test.sesion",clave);
                gerente.Login("test.sesion",clave);
                var empleados=new EmpleadoServicio(new EmpleadoDatos(db),gerente);
                int empleado=empleados.Guardar(new Empleado { CodigoEmpleado="TEST-REVOCAR",Nombres="Usuario",Apellidos="Temporal" });
                int rol=gerente.Roles().Single(r=>r.Nombre=="Digitador").RolID;
                var cuenta=new Usuario { EmpleadoID=empleado,RolID=rol,NombreUsuario="test.revocar" };
                cuenta.UsuarioID=gerente.GuardarUsuario(cuenta,clave);
                colaborador.Login(cuenta.NombreUsuario,clave);
                gerente.ActivarUsuario(cuenta.UsuarioID,false);
                Rechaza(()=>colaborador.Actual(),"sesion abierta revocada al desactivar usuario");
                gerente.ActivarUsuario(cuenta.UsuarioID,true);
                colaborador.Login(cuenta.NombreUsuario,clave);
                cuenta.RolID=gerente.Roles().Single(r=>r.Nombre=="Cajero").RolID;
                gerente.GuardarUsuario(cuenta,null);
                Rechaza(()=>colaborador.ExigirMantenimiento(),"cambio de rol revalidado sin nuevo login");
                empleados.CambiarActivo(empleado,false);
                Rechaza(()=>colaborador.Login(cuenta.NombreUsuario,clave),"empleado inactivo no inicia sesion");
            }
            // Segundo intento de bootstrap: su ROLLBACK aborta la transaccion temporal completa.
            using(var scope=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(1)))
            using(var app=new Aplicacion(cadena))
            {
                app.Seguridad.Configurar("TEST-BOOT","Prueba","Temporal","test.boot",clave);
                Rechaza(()=>app.Seguridad.Configurar("TEST-BOOT2","Prueba","Temporal","test.boot2",clave),"segundo bootstrap rechazado");
            }
            Comprobar(ConteoUsuarios(cadena)==0,"bootstrap duplicado no deja basura");
            Console.WriteLine($"Resultado: {pruebas} comprobaciones correctas. Capturas: {Capturas}");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex.GetType().Name+": "+ex.Message); return 1; }
    }
    private static void SeleccionarPrimero(Form f)
    {
        var grid=Control<DataGridView>(f,"Tabla");
        if(grid.Rows.Count==0) throw new Exception("No hay filas para seleccionar.");
        grid.CurrentCell=grid.Rows[0].Cells.Cast<DataGridViewCell>().First(c=>c.Visible); grid.Rows[0].Selected=true; Application.DoEvents();
    }
}
