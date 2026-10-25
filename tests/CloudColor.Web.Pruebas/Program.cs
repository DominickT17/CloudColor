using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using CloudColor.Negocio;
using CloudColor.Entidades;

internal static partial class Program
{
    private static int pruebas;
    private static readonly string Clave=Environment.GetEnvironmentVariable("CLOUDCOLOR_QA_PASSWORD")
        ?? throw new InvalidOperationException("Defina CLOUDCOLOR_QA_PASSWORD solo para ejecutar las pruebas QA.");
    private static void Check(bool ok,string mensaje) { if(!ok)throw new Exception(mensaje);Console.WriteLine("OK WEB: "+mensaje);pruebas++; }
    private static HttpClient Cliente()=>new(new HttpClientHandler { AllowAutoRedirect=false,CookieContainer=new CookieContainer() }) { BaseAddress=new Uri("http://localhost:5256"),Timeout=TimeSpan.FromSeconds(30) };
    private static async Task<string> Pagina(HttpClient c,string ruta)
    {
        using var r=await c.GetAsync(ruta); Check(r.StatusCode==HttpStatusCode.OK,"GET "+ruta);return await r.Content.ReadAsStringAsync();
    }
    private static async Task<HttpResponseMessage> Post(HttpClient c,string formulario,string ruta,Dictionary<string,string> datos)
    {
        var html=await Pagina(c,formulario);
        var m=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if(!m.Success)throw new Exception("No hay antiforgery en "+formulario);
        datos["__RequestVerificationToken"]=WebUtility.HtmlDecode(m.Groups[1].Value);
        return await c.PostAsync(ruta,new FormUrlEncodedContent(datos));
    }
    private static async Task Login(HttpClient c,string nombre)
    {
        using var r=await Post(c,"/Login","/Login",new(){{"usuario",nombre},{"password",Clave}});
        Check(r.StatusCode==HttpStatusCode.Redirect && r.Headers.Location?.ToString()=="/Dashboard","login "+nombre);
    }
    private static async Task<int> Main(string[] args)
    {
        try
        {
            // No enviar solicitudes a otro servidor que pudiera ocupar el puerto QA.
            var puerto=new System.Net.Sockets.TcpListener(IPAddress.Loopback,5256);
            try { puerto.Start(); } finally { puerto.Stop(); }
            using var entorno=new EntornoPruebas("Server=.\\SQLEXPRESS;Database=CloudColorDB;Integrated Security=True;TrustServerCertificate=True");
            using(var a=new Aplicacion(entorno.Cadena))
            {
                a.Seguridad.Configurar("WEB-G","Gerente","Pruebas web","web.gerente",Clave);a.Seguridad.Login("web.gerente",Clave);
                foreach(string rol in new[]{"Digitador","Cajero"})
                {
                    int e=a.Empleados.Guardar(new Empleado{CodigoEmpleado="WEB-"+rol,Nombres=rol,Apellidos="Pruebas web"});
                    a.Seguridad.GuardarUsuario(new Usuario{EmpleadoID=e,RolID=a.Seguridad.Roles().Single(x=>x.Nombre==rol).RolID,NombreUsuario="web."+rol.ToLowerInvariant()},Clave);
                }
            }
            var raiz=new DirectoryInfo(AppContext.BaseDirectory);while(raiz!=null&&!Directory.Exists(Path.Combine(raiz.FullName,"src","CloudColor.Web")))raiz=raiz.Parent;
            if(raiz==null)throw new Exception("No se encontro el repositorio.");
            var inicio=new ProcessStartInfo("dotnet") { WorkingDirectory=Path.Combine(raiz.FullName,"src","CloudColor.Web"),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
            inicio.ArgumentList.Add(Path.Combine(inicio.WorkingDirectory,"bin","Release","net9.0","CloudColor.Web.dll"));
            inicio.Environment["ConnectionStrings__CloudColor"]=entorno.Cadena;inicio.Environment["ASPNETCORE_ENVIRONMENT"]="Development";inicio.Environment["ASPNETCORE_URLS"]="http://localhost:5256";
            using var proceso=Process.Start(inicio)??throw new Exception("No se pudo iniciar Kestrel.");
            proceso.BeginOutputReadLine();proceso.BeginErrorReadLine();
            try
            {
                using var c=Cliente();bool listo=false;
                for(int i=0;i<40;i++){try { using var r=await c.GetAsync("/Login"); if(r.StatusCode==HttpStatusCode.OK){listo=true;break;} }catch(HttpRequestException){} await Task.Delay(250);}
                Check(listo&&!proceso.HasExited,"Kestrel en base temporal");
                using(var r=await c.GetAsync("/Dashboard"))Check(r.StatusCode==HttpStatusCode.Redirect,"acceso anonimo redirige a login");
                using(var r=await c.PostAsync("/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"usuario","web.gerente"},{"password",Clave}})))Check(r.StatusCode==HttpStatusCode.BadRequest,"POST sin antiforgery rechazado");
                using(var r=await Post(c,"/Login","/Login",new(){{"usuario","web.gerente"},{"password","Incorrecta-temporal!"}}))Check(r.StatusCode==HttpStatusCode.OK&&!(await r.Content.ReadAsStringAsync()).Contains("Incorrecta-temporal!"),"login incorrecto no refleja contraseña");
                await Login(c,"web.gerente");
                foreach(var ruta in new[]{"/Dashboard","/Clientes","/Proveedores","/Productos","/Variantes","/Bodegas","/Ubicaciones"})
                {
                    string html=await Pagina(c,ruta);Check(!html.Contains("PasswordHash")&&!html.Contains("PBKDF2$")&&!html.Contains("ConnectionString"),"sin secretos en "+ruta);
                }
                using(var r=await Post(c,"/Clientes/Editar","/Clientes/Editar/0",new(){{"NombreRazonSocial","Cliente web temporal"},{"TipoCliente","Particular"},{"Correo","prueba@example.test"}}))Check(r.StatusCode==HttpStatusCode.Redirect,"crear cliente HTTP");
                using(var a=new Aplicacion(entorno.Cadena))
                {
                    a.Seguridad.Login("web.gerente",Clave);var cliente=a.Clientes.Listar().Single();
                    using(var r=await Post(c,"/Clientes/Editar/"+cliente.ClienteID,"/Clientes/Editar/"+cliente.ClienteID,new(){{"NombreRazonSocial","Cliente web editado"},{"TipoCliente","Empresa"}}))Check(r.StatusCode==HttpStatusCode.Redirect,"editar cliente HTTP");
                    using(var r=await Post(c,"/Clientes","/Clientes/Estado/"+cliente.ClienteID,new(){{"activo","false"}}))Check(r.StatusCode==HttpStatusCode.Redirect,"desactivar cliente HTTP");
                    Check(!a.Clientes.Listar().Single().Activo,"estado persistido por Negocio");a.Clientes.CambiarActivo(cliente.ClienteID,true);
                }
                using var cajero=Cliente();await Login(cajero,"web.cajero");
                using(var r=await cajero.GetAsync("/Clientes"))Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccesoDenegado"),"Cajero no entra por URL a catalogos");
                await ProbarOperaciones(c,cajero,entorno.Cadena);
                if(args.Contains("--visual")){Console.WriteLine("QA visual disponible en http://localhost:5256. Cuenta temporal web.gerente. Crear el archivo de cierre indicado para eliminar QA.");string cierre=Path.Combine(Path.GetTempPath(),"CloudColor-Web-QA-"+Environment.ProcessId+".stop");Console.WriteLine("Para cerrar QA, crear: "+cierre);var limite=DateTime.UtcNow.AddMinutes(30);while(!File.Exists(cierre)&&DateTime.UtcNow<limite)await Task.Delay(500);}
                using(var r=await Post(c,"/Dashboard","/Salir",new()))Check(r.StatusCode==HttpStatusCode.Redirect,"logout POST");
                using(var r=await c.GetAsync("/Dashboard"))Check(r.StatusCode==HttpStatusCode.Redirect,"cookie revocada al salir");
            }
            finally { if(!proceso.HasExited){proceso.Kill(true);await proceso.WaitForExitAsync();} }
            entorno.VerificarOriginal();Console.WriteLine($"Resultado WEB: {pruebas} comprobaciones correctas.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FALLO WEB: "+ex.Message);return 1;}
    }
}
